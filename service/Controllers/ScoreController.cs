/*---------------------------------------------------------------------------------------------
*  Copyright (c) Nicolas Jinchereau. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace ShowdownSoftware
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

    namespace Controllers
    {
        [ApiController, Route("scores")]
        public class ScoreController : ControllerBase
        {
            // These are just base64 encodings of pairs of UUIDs.
            // The public key only grants access to 'add' and 'get'
            // endpoints. For deletion, the private key is required,
            // which limits the risk of including the public key
            // in application code.
            public const string PublicKey = "ZTVlODU0ODItMTZiNi00ODg1LWI3Y2QtMGZiZmYyYWUzMWQzMTI2NzFiNzgtNGRjZS00NWI5LWE3MGMtZWFkMjViYTEyMGNj";
            public const string PrivateKey = "ZZGIxZTUwOTEtY2JkOS00NTI1LWE3NTEtYWVhOGRiNjhhNWRkZDlkMzdiNjQtNjVjYS00ZjdiLWExYWYtZDA0NDM5ZDlhZjg4";

            HighScoreService highScores;

            public ScoreController(HighScoreService highScores)
            {
                this.highScores = highScores;
            }

            [HttpGet("info")]
            public string GetInfo()
            {
                return "Score Service 1.1.0";
            }

            [HttpPost("add")]
            public AddScoreResponse Add([FromBody] AddScoreRequest request)
            {
                var response = new AddScoreResponse();

                try
                {
                    if (request == null)
                        throw new Exception("request is null - input format may be incorrect");

                    if (request.publicKey != PublicKey)
                        throw new Exception("invalid access key");

                    if (string.IsNullOrEmpty(request.game))
                        throw new Exception("no game was specified");

                    if (string.IsNullOrEmpty(request.name))
                        throw new Exception("no player name was specified");

                    if (request.score <= 0)
                        throw new Exception("invalid score: score must be >= 0");

                    response.scoreId = highScores.AddScore(request.game, request.name, request.score, request.date);
                    response.status = 0;
                }
                catch (Exception ex)
                {
                    response.status = 1;
                    response.message = ex.ToString();
                }

                return response;
            }

            [HttpPost("get")]
            public GetScoresResponse Get([FromBody] GetScoresRequest request)
            {
                var response = new GetScoresResponse();

                try
                {
                    if (request == null)
                        throw new Exception("request is null - input format may be incorrect");

                    if (request.publicKey != PublicKey)
                        throw new Exception("invalid access key");

                    if (string.IsNullOrEmpty(request.game))
                        throw new Exception("no game was specified");

                    var maxCount = request.maxCount > 0 ? request.maxCount : int.MaxValue;
                    response.scores = highScores.GetScores(request.game, maxCount);
                    response.status = 0;
                }
                catch (Exception ex)
                {
                    response.status = 1;
                    response.message = ex.ToString();
                }

                return response;
            }

            [HttpPost("delete")]
            public DeleteScoreResponse Delete([FromBody] DeleteScoreRequest request)
            {
                var response = new DeleteScoreResponse();

                try
                {
                    if (request == null)
                        throw new Exception("request is null - input format may be incorrect");

                    if (request.privateKey != PrivateKey)
                        throw new Exception("invalid access key");

                    if (string.IsNullOrEmpty(request.game))
                        throw new Exception("no game was specified");

                    if (request.id <= 0)
                        throw new Exception("invalid record id");

                    highScores.DeleteScore(request.game, request.id);
                    response.status = 0;
                }
                catch (Exception ex)
                {
                    response.status = 1;
                    response.message = ex.Message;
                }

                return response;
            }
        }
    }
}
