/*---------------------------------------------------------------------------------------------
*  Copyright (c) Nicolas Jinchereau. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;

namespace ShowdownSoftware
{
    public interface IProxyTarget
    {
        object Execute(MethodInfo method, object[] args);
    }

    public class ProxyFactory
    {
        // Produces an implementation of T where all methods
        // are forwarded to proxyTarget.Execute.
        public static T Create<T>(IProxyTarget proxyTarget) where T : class
        {
            if(!typeof(T).IsInterface)
                throw new Exception("'T' must be an interface");

            var interfaceType = typeof(T);

            var assemblyName = new AssemblyName(Guid.NewGuid().ToString());
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(interfaceType.Name + "Implementation");

            var typeBuilder = moduleBuilder.DefineType(interfaceType.Name + "Implementation",
                                                       TypeAttributes.Public |
                                                       TypeAttributes.Class |
                                                       TypeAttributes.AutoClass |
                                                       TypeAttributes.UnicodeClass |
                                                       TypeAttributes.AutoLayout,
                                                       null);

            FieldBuilder proxyTargetField = typeBuilder.DefineField("proxyTarget", typeof(IProxyTarget), FieldAttributes.Public);

            typeBuilder.AddInterfaceImplementation(interfaceType);

            var methodInfoFields = new List<FieldBuilder>();
            var methodsInfos = new List<MethodBuilder>();

            var interfaceMethods = interfaceType.GetMethods();

            for(int m = 0; m < interfaceMethods.Length; ++m)
            {
                var interfaceMethod = interfaceMethods[m];

                var methodInfoField = typeBuilder.DefineField("methodInfo_" + m, typeof(MethodInfo), FieldAttributes.Public);
                methodInfoFields.Add(methodInfoField);

                var parameters = interfaceMethod.GetParameters();
                var paramCount = parameters.Length;
                var paramTypes = parameters.Select(p => p.ParameterType).ToArray();

                var methodInfo = typeBuilder.DefineMethod(
                    interfaceMethod.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    interfaceMethod.ReturnType, paramTypes);

                methodsInfos.Add(methodInfo);

                var gen = methodInfo.GetILGenerator();

                LocalBuilder L0_args = gen.DeclareLocal(typeof(object[]));

                gen.Emit(OpCodes.Ldc_I4, paramCount);
                gen.Emit(OpCodes.Newarr, typeof(object));

                for (int i = 0; i < paramCount; ++i)
                {
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Ldc_I4, i);
                    gen.Emit(OpCodes.Ldarg, i + 1);

                    if (paramTypes[i].IsValueType)
                        gen.Emit(OpCodes.Box, paramTypes[i]);

                    gen.Emit(OpCodes.Stelem_Ref);
                }

                gen.Emit(OpCodes.Stloc_0); // store object[] args

                gen.Emit(OpCodes.Ldarg_0); // load 'this'
                gen.Emit(OpCodes.Ldfld, proxyTargetField); // load executor field

                gen.Emit(OpCodes.Ldarg_0); // load 'this'
                gen.Emit(OpCodes.Ldfld, methodInfoField); // load methodInfo_# field

                gen.Emit(OpCodes.Ldloc_0); // load object[] args
                gen.Emit(OpCodes.Callvirt, typeof(IProxyTarget).GetMethod("Execute"));

                // return the value casted to the correct type
                if(interfaceMethod.ReturnType != typeof(void))
                {
                    gen.Emit(OpCodes.Unbox_Any, interfaceMethod.ReturnType);
                    gen.Emit(OpCodes.Ret); // return unboxed result
                }
                else
                {
                    gen.Emit(OpCodes.Pop); // pop Execute()'s unused return value
                    gen.Emit(OpCodes.Ret); // return nothing
                }

                typeBuilder.DefineMethodOverride(methodInfo, interfaceType.GetMethod(interfaceMethod.Name));
            }

            var instanceType = typeBuilder.CreateType();
            var instance = (T)Activator.CreateInstance(instanceType);

            instanceType.GetField("proxyTarget").SetValue(instance, proxyTarget);

            for (int i = 0; i < methodsInfos.Count; ++i)
            {
                var methodName = methodsInfos[i].Name;
                var methodParamTypes = methodsInfos[i].GetParameters().Select(p => p.ParameterType).ToArray();

                var methodInfo = interfaceType.GetMethod(methodName, methodParamTypes);
                instanceType.GetField("methodInfo_" + i).SetValue(instance, methodInfo);
            }

            return instance;
        }
    }
}
