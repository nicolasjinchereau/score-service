/*---------------------------------------------------------------------------------------------
*  Copyright (c) Nicolas Jinchereau. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using JsonFx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ScoreClient
{
    public class HighScore
    {
        public long id;
        public string name;
        public int score;
        public DateTime date;
    }

    public class AddScoreRequest
    {
        public string publicKey;
        public string game;
        public string name;
        public int score;
        public DateTime date;
    }

    public class AddScoreResponse
    {
        public int status;
        public string message;
        public long scoreId;
    }

    public class GetScoresRequest
    {
        public string publicKey;
        public string game;
        public int maxCount;
    }

    public class GetScoresResponse
    {
        public int status;
        public string message;
        public List<HighScore> scores;
    }

    public class DeleteScoreRequest
    {
        public string privateKey;
        public string game;
        public long id;
    }

    public class DeleteScoreResponse
    {
        public int status;
        public string message;
    }

    class Program
    {
        const string PublicKey = "ZTVlODU0ODItMTZiNi00ODg1LWI3Y2QtMGZiZmYyYWUzMWQzMTI2NzFiNzgtNGRjZS00NWI5LWE3MGMtZWFkMjViYTEyMGNj";
        const string PrivateKey = "ZZGIxZTUwOTEtY2JkOS00NTI1LWE3NTEtYWVhOGRiNjhhNWRkZDlkMzdiNjQtNjVjYS00ZjdiLWExYWYtZDA0NDM5ZDlhZjg4";
        const string ServiceURL = "http://www.example.com/scores";
        const string GameName = "my-game";

        const string Help =
            "Usage:\n" +
            "  'help'\n" +
            "  'add <name> <score>'\n" +
            "  'get'\n" +
            "  'delete <score id>'\n" +
            "  'exit'\n";
        
        const string InvalidInputMessage = "Invalid input. Please try again.";

        static void Main(string[] args)
        {
            Console.WriteLine(Help);
            
            Console.Write("> ");
            var input = Console.ReadLine();

            while(input != "exit")
            {
                try
                {
                    if(input == "help")
                    {
                        Console.WriteLine(Help);
                    }
                    else if (input.StartsWith("info"))
                    {
                        Console.WriteLine($"info: {GetInfo()}");
                    }
                    else if (input.StartsWith("add"))
                    {
                        var match = Regex.Match(input, @"add\s+(\w*)\s+([0-9]+)");
                        if(match == null)
                            throw new Exception(InvalidInputMessage);

                        var name = match.Groups[1].Value;
                        var score = int.Parse(match.Groups[2].Value);
                        long id = AddScore(name, score);
                        Console.WriteLine($"added score: ({id})");
                    }
                    else if(input == "get")
                    {
                        var scores = GetScores();
                        Console.WriteLine(" Scores:");

                        foreach(var score in scores)
                            Console.WriteLine($"  {score.id}) {score.name}: {score.score} - {score.date.ToString("yyyy-MM-dd HH:mm:ss")}");
                    }
                    else if(input.StartsWith("delete"))
                    {
                        var match = Regex.Match(input, @"delete\s+([0-9]+)");
                        if(match == null)
                            throw new Exception(InvalidInputMessage);

                        var id = int.Parse(match.Groups[1].Value);
                        DeleteScore(id);
                        Console.WriteLine($"deleted score: ({id})");
                    }
                    else
                    {
                        throw new Exception(InvalidInputMessage);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Console.Write("> ");
                input = Console.ReadLine();
            }
        }

        static List<HighScore> GetScores()
        {
            var request = new GetScoresRequest() {
                publicKey = PublicKey,
                game = GameName,
                maxCount = 0 // no limit
            };
            
            string response = PostObject(ServiceURL + "/get", request);
            
            var obj = JsonReader.Deserialize<GetScoresResponse>(response, ReaderSettings);
            if(obj.status != 0)
                throw new Exception(obj.message);

            return obj.scores;
        }

        static string GetInfo()
        {
            return GetContent(ServiceURL + "/info");
        }

        static long AddScore(string name, int score)
        {
            var request = new AddScoreRequest() {
                publicKey = PublicKey,
                game = GameName,
                name = name,
                score = score,
                date = DateTime.Now
            };
            
            string response = PostObject(ServiceURL + "/add", request);
            
            var obj = JsonReader.Deserialize<AddScoreResponse>(response, ReaderSettings);
            if(obj.status != 0)
                throw new Exception(obj.message);

            return obj.scoreId;
        }

        static void DeleteScore(long id)
        {
            var request = new DeleteScoreRequest() {
                privateKey = PrivateKey,
                game = GameName,
                id = id
            };
            
            string response = PostObject(ServiceURL + "/delete", request);
            
            var obj = JsonReader.Deserialize<DeleteScoreResponse>(response, ReaderSettings);
            if(obj.status != 0)
                throw new Exception(obj.message);
        }

        static readonly JsonWriterSettings WriterSettings = new JsonWriterSettings()
        {
            DateTimeSerializer = (JsonWriter writer, DateTime value) => {
                writer.Write(value.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        };

        static readonly JsonReaderSettings ReaderSettings = new JsonReaderSettings()
        {
            DateTimeDeserializer = (JsonReader reader) => {
                var str = (string)reader.Read(typeof(string), false);
                return DateTime.Parse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            }
        };

        static string GetContent(string url)
        {
            var req = WebRequest.CreateHttp(url);
            req.Method = "GET";

            var response = req.GetResponse() as HttpWebResponse;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Request failed: " + response.StatusCode);

            var reader = new StreamReader(response.GetResponseStream());
            return reader.ReadToEnd();
        }

        static string PostObject(string url, object obj)
        {
            var json = JsonWriter.Serialize(obj, WriterSettings);

            var req = WebRequest.CreateHttp(url);
            req.Method = "POST";
            req.ContentType = "application/json";
            
            var writer = new BinaryWriter(req.GetRequestStream());
            writer.Write(Encoding.UTF8.GetBytes(json)); 
            writer.Flush();
            
            var response = req.GetResponse() as HttpWebResponse;
            if(response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Request failed: " + response.StatusCode);

            var reader = new StreamReader(response.GetResponseStream());
            return reader.ReadToEnd();
        }
    }
}
