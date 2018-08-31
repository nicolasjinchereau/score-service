/*---------------------------------------------------------------------------------------------
*  Copyright (c) Nicolas Jinchereau. All rights reserved.
*  Licensed under the MIT License. See License.txt in the project root for license information.
*--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace ShowdownSoftware
{
    public interface HighScoreService
    {
        [SqlQuery("insert into `scores` (`game`, `name`, `score`, `date`) values(@game, @name, @score, @date); select LAST_INSERT_ID();")]
        int AddScore(string game, string name, int score, DateTime date);

        [SqlQuery("select `id`, `name`, `score`, `date` from `scores` where `game`=@game order by `score` desc limit @max")]
        List<HighScore> GetScores(string game, int max);

        [SqlQuery("delete from `scores` where `game`=@game and `id`=@id")]
        void DeleteScore(string game, long id);
    }
}
