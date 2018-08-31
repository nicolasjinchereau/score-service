/*---------------------------------------------------------------------------------------------
*  Copyright (c) Nicolas Jinchereau. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ShowdownSoftware
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SqlQueryAttribute : Attribute
    {
        public string Command { get; private set; }

        public SqlQueryAttribute(string command) {
            this.Command = command;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class SqlUpdateAttribute : Attribute
    {
        public string Command { get; private set; }

        public SqlUpdateAttribute(string command) {
            this.Command = command;
        }
    }

    /// <summary>Method parameters bind to '@&lt;param name&gt;' by default. Mapping can be overriden by adding a BindAttribute to the parameter.</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class BindAttribute : Attribute
    {
        public string Placeholder { get; private set; }

        public BindAttribute(string placeholder) {
            this.Placeholder = placeholder;
        }
    }

    public class SqlProxyTarget : IProxyTarget
    {
        readonly string connectionString;

        public SqlProxyTarget(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public object Execute(MethodInfo method, object[] args)
        {
            foreach(var att in method.GetCustomAttributes(true))
            {
                if(att is SqlUpdateAttribute)
                {
                    PerformUpdate((SqlUpdateAttribute)att, method, args);
                    return null;
                }
                else if(att is SqlQueryAttribute)
                {
                    return PerformQuery((SqlQueryAttribute)att, method, args);
                }
            }
   
            return null;
        }

        void PerformUpdate(SqlUpdateAttribute update, MethodInfo method, object[] args)
        {
            using(var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using(var cmd = new MySqlCommand(update.Command, connection))
                {
                    BindInputParams(cmd, method, args);

                    if (cmd.ExecuteNonQuery() == 0)
                        throw new Exception("operation failed");
                }
            }
        }

        object PerformQuery(SqlQueryAttribute query, MethodInfo method, object[] args)
        {
            using(var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using(var cmd = new MySqlCommand(query.Command, connection))
                {
                    BindInputParams(cmd, method, args);

                    using(var reader = cmd.ExecuteReader())
                    {
                        if(!reader.Read())
                            return null;

                        var returnType = method.ReturnType;
                        var fieldCount = reader.FieldCount;
                        var fieldType = reader.GetFieldType(0);

                        if(!HasInterface<IList>(returnType))
                        {
                            var value = reader.GetValue(0);

                            if(value.GetType() != returnType)
                                value = Convert.ChangeType(value, returnType);

                            return value;
                        }
                        else if(fieldCount == 1 && fieldType.IsArray && CanConvertElements(fieldType, returnType))
                        {
                            var returnElemType = GetElementType(returnType);
                            var fieldElemType = GetElementType(fieldType);

                            var values = reader.GetValue(0) as IList;

                            IList ret = null;

                            if(returnType.IsArray)
                            {
                                if(returnType == fieldType)
                                {
                                    ret = values;
                                }
                                else
                                {
                                    ret = Array.CreateInstance(returnElemType, values.Count);

                                    for(int i = 0; i < values.Count; ++i)
                                        ret[i] = Convert.ChangeType(values[i], returnElemType);
                                }
                            }
                            else
                            {
                                ret = Activator.CreateInstance(typeof(List<>).MakeGenericType(returnElemType)) as IList;

                                foreach(var val in values)
                                {
                                    var value = val;

                                    if(fieldElemType != returnElemType)
                                        value = Convert.ChangeType(value, returnElemType);

                                    ret.Add(value);
                                }
                            }

                            return ret;
                        }
                        else
                        {
                            var returnElemType = GetElementType(returnType);
                            IList ret = Activator.CreateInstance(typeof(List<>).MakeGenericType(returnElemType)) as IList;

                            do
                            {
                                var obj = CreateObject(reader, returnElemType);
                                ret.Add(obj);

                            } while (reader.Read());

                            if(returnType.IsArray)
                            {
                                IList arr = Array.CreateInstance(returnElemType, ret.Count) as IList;

                                for(int i = 0; i < ret.Count; ++i)
                                    arr[i] = ret[i];

                                ret = arr;
                            }

                            return ret;
                        }
                    }
                }
            }
        }

        void BindInputParams(MySqlCommand cmd, MethodInfo method, object[] args)
        {
            var requiredParams = GetRequiredParams(cmd.CommandText);
            var providedParams = GetProvidedParams(method);

            foreach(var req in requiredParams)
            {
                int index = providedParams.FindIndex(p => p == req);
                if(index == -1)
                    throw new Exception($"No argument found for parameter '{req}' in command '{cmd.CommandText}'");

                cmd.Parameters.AddWithValue(providedParams[index], args[index]);
            }
        }

        List<string> GetRequiredParams(string commandText)
        {
            var parameters = new List<string>();
            var tokenizer = new MySqlTokenizer(commandText);
            var param = tokenizer.NextParameter();

            while (param != null)
            {
                if (param == "?")
                    throw new Exception("'?' parameters are not supported");

                parameters.Add(param);
                param = tokenizer.NextParameter();
            }

            return parameters;
        }

        List<string> GetProvidedParams(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var ret = new List<string>(parameters.Length);

            foreach(var param in parameters)
            {
                var attrib = param.GetCustomAttribute<BindAttribute>();
                if (attrib != null)
                    ret.Add(attrib.Placeholder);
                else
                    ret.Add($"@{param.Name}");
            }

            return ret;
        }

        object CreateObject(MySqlDataReader reader, Type type)
        {
            var obj = Activator.CreateInstance(type);

            int fieldCount = reader.FieldCount;

            for(int i = 0; i < fieldCount; ++i)
            {
                var colName = reader.GetName(i);

                var field = type.GetField(colName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if(field == null)
                {
                    Console.WriteLine($"warning: field not found for '{colName}' of type '{reader.GetFieldType(i)}'");
                    continue;
                }

                var value = reader.GetValue(i);
                if(value.GetType() != field.FieldType)
                {
                    try {
                        value = Convert.ChangeType(value, field.FieldType);
                    }
                    catch(Exception)
                    {
                        Console.WriteLine($"warning: field type mismatch for '{colName}' - need '{field.FieldType}' not '{value.GetType()}'");
                        continue;
                    }
                }

                field.SetValue(obj, value);
            }

            return obj;
        }

        Type GetElementType(IList list)
        {
            return GetElementType(list);
        }

        Type GetElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();
            else
                return type.GetGenericArguments()[0];
        }

        bool CanConvertElements(Type from, Type to)
        {
            if (!from.HasElementType || !to.HasElementType)
                return false;

            var fromElemType = GetElementType(from);
            var toElemType = GetElementType(to);

            return fromElemType == toElemType || TypeDescriptor.GetConverter(from).CanConvertTo(to);
        }

        bool HasInterface<T>(object obj)
        {
            return HasInterface<T>(obj.GetType());
        }

        bool HasInterface<T>(Type type)
        {
            return typeof(T).IsAssignableFrom(type);
        }
    }
}
