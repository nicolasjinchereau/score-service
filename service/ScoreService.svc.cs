/*---------------------------------------------------------------------------------------------
*  Copyright (c) Nicolas Jinchereau. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using MySql.Data.MySqlClient;

namespace ShowdownSoftware
{
    [DataContract]
    public class HighScore
    {
        [DataMember] public long id;
        [DataMember] public string name;
        [DataMember] public int score;
    }

    [DataContract]
    public class AddScoreRequest
    {
        [DataMember] public string publicKey;
        [DataMember] public string game;
        [DataMember] public string name;
        [DataMember] public int score;
        [DataMember] public DateTime date;
    }
    
    [DataContract]
    public class AddScoreResponse
    {
        [DataMember] public int status;
        [DataMember] public string message;
        [DataMember] public long scoreId;
    }

    [DataContract]
    public class GetScoresRequest
    {
        [DataMember] public string publicKey;
        [DataMember] public string game;
        [DataMember] public int maxCount;
    }
    
    [DataContract]
    public class GetScoresResponse
    {
        [DataMember] public int status;
        [DataMember] public string message;
        [DataMember] public List<HighScore> scores;
    }
    
    [DataContract]
    public class DeleteScoreRequest
    {
        [DataMember] public string privateKey;
        [DataMember] public string game;
        [DataMember] public long id;
    }

    [DataContract]
    public class DeleteScoreResponse
    {
        [DataMember] public int status;
        [DataMember] public string message;
    }
    
    [ServiceContract, AspNetCompatibilityRequirements(
        RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class ScoreService
    {
        // These are just base64 encodings of pairs of UUIDs.
        // The public key only grants access to 'add' and 'get'
        // endpoints. For deletion, the private key is required,
        // which limits the risk of including the public key
        // in application code.
        public const string PublicKey = "ZTVlODU0ODItMTZiNi00ODg1LWI3Y2QtMGZiZmYyYWUzMWQzMTI2NzFiNzgtNGRjZS00NWI5LWE3MGMtZWFkMjViYTEyMGNj";
        public const string PrivateKey = "ZZGIxZTUwOTEtY2JkOS00NTI1LWE3NTEtYWVhOGRiNjhhNWRkZDlkMzdiNjQtNjVjYS00ZjdiLWExYWYtZDA0NDM5ZDlhZjg4";
        
        // Only one table is required, which can be created by the statement below.
        public const string CreateTableStatement = @"
CREATE TABLE `scores` (
    `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
    `game` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
    `name` varchar(32) COLLATE utf8mb4_unicode_ci NOT NULL,
    `score` int(10) unsigned NOT NULL,
    `date` datetime NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `id` (`id`)
) ENGINE=MyISAM AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

        [OperationContract, WebInvoke(
            UriTemplate = "add",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        public AddScoreResponse Add(AddScoreRequest request)
        {
            var response = new AddScoreResponse();
            
            try
            {
                if(request == null)
                    throw new Exception("request is null - input format may be incorrect");
                
                if(request.publicKey != PublicKey)
                    throw new Exception("invalid access key");

                if(string.IsNullOrEmpty(request.game))
                    throw new Exception("no game was specified");

                if(string.IsNullOrEmpty(request.name))
                    throw new Exception("no player name was specified");

                if(request.score <= 0)
                    throw new Exception("invalid score: score must be >= 0");

                var connStr = ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString;
                using(var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    
                    var query = "insert into `scores` (`game`, `name`, `score`, `date`) values(@game, @name, @score, @date)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@game", request.game);
                    cmd.Parameters.AddWithValue("@name", request.name);
                    cmd.Parameters.AddWithValue("@score", request.score);
                    cmd.Parameters.AddWithValue("@date", request.date);

                    if(cmd.ExecuteNonQuery() == 0)
                        throw new Exception("operation failed");

                    response.status = 0;
                    response.message = null;
                    response.scoreId = cmd.LastInsertedId;
                }
            }
            catch(Exception ex)
            {
                response.status = 1;
                response.message = ex.Message;
            }

            return response;
        }
        
        [OperationContract, WebInvoke(
            UriTemplate = "get",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        public GetScoresResponse Get(GetScoresRequest request)
        {
            var response = new GetScoresResponse();

            try
            {
                if(request == null)
                    throw new Exception("request is null - input format may be incorrect");
                
                if(request.publicKey != PublicKey)
                    throw new Exception("invalid access key");

                if(string.IsNullOrEmpty(request.game))
                    throw new Exception("no game was specified");

                var connStr = ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString;
                using(var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    
                    var scores = new List<HighScore>();
                    var query = "select `id`, `name`, `score` from `scores` where `game`=@game order by `score` desc";

                    if(request.maxCount > 0) {
                        query += " limit " + request.maxCount;
                        scores.Capacity = request.maxCount;
                    }

                    var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("game", request.game);

                    var reader = cmd.ExecuteReader();
                    while(reader.Read())
                    {
                        var score = new HighScore();
                        score.id = reader.GetInt32("id");
                        score.name = reader.GetString("name");
                        score.score = reader.GetInt32("score");
                        scores.Add(score);
                    }

                    response.status = 0;
                    response.scores = scores;
                }
            }
            catch(Exception ex)
            {
                response.status = 1;
                response.message = ex.Message;
            }

            return response;
        }
        
        [OperationContract, WebInvoke(
            UriTemplate = "delete",
            Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Bare)]
        public DeleteScoreResponse Delete(DeleteScoreRequest request)
        {
            var response = new DeleteScoreResponse();

            try
            {
                if(request == null)
                    throw new Exception("request is null - input format may be incorrect");
                
                if(request.privateKey != PrivateKey)
                    throw new Exception("invalid access key");

                if(string.IsNullOrEmpty(request.game))
                    throw new Exception("no game was specified");

                if(request.id <= 0)
                    throw new Exception("invalid record id");

                var connStr = ConfigurationManager.ConnectionStrings["LocalMySqlServer"].ConnectionString;
                using(var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    
                    var query = "delete from `scores` where `game`=@game and `id`=@id";
                    var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("game", request.game);
                    cmd.Parameters.AddWithValue("id", request.id);
                    
                    if(cmd.ExecuteNonQuery() == 0)
                        throw new Exception("operation failed");

                    response.status = 0;
                    response.message = null;
                }
            }
            catch(Exception ex)
            {
                response.status = 1;
                response.message = ex.Message;
            }

            return response;
        }
    }
}
