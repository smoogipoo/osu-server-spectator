// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Dapper;
using osu.Game.Scoring.Legacy;
using osu.Server.QueueProcessor;
using osu.Server.Spectator.Database;
using osu.Server.Spectator.Hubs;

namespace osu.Server.Spectator
{
    public class ReplayQueueProcessor : QueueProcessor<ReplayQueueItem>
    {
        public ReplayQueueProcessor()
            : base(new QueueConfiguration { InputQueueName = "scores-pending-replays" })
        {
            DapperExtensions.InstallDateTimeOffsetMapper();
        }

        protected override void ProcessResult(ReplayQueueItem item)
        {
            using (var conn = GetDatabaseConnection())
            {
                long? scoreId = conn.QuerySingleOrDefault<long?>("SELECT `score_id` FROM `solo_score_tokens` WHERE `id` = @Id", new
                {
                    Id = item.Score.ScoreInfo.OnlineID
                });

                // Todo: Requeue after X-seconds? How?
                if (scoreId == null)
                    throw new InvalidOperationException("Score ID is null.");

                var score = item.Score;
                var scoreInfo = item.Score.ScoreInfo;
                var legacyEncoder = new LegacyScoreEncoder(score, null);

                string path = Path.Combine(SpectatorHub.REPLAYS_PATH, scoreInfo.Date.Year.ToString(), scoreInfo.Date.Month.ToString(), scoreInfo.Date.Day.ToString());

                Directory.CreateDirectory(path);

                string filename = $"replay-{scoreInfo.Ruleset.ShortName}_{scoreInfo.BeatmapInfo.OnlineID}_{score.ScoreInfo.OnlineID}.osr";

                Console.WriteLine($"Writing replay for score {score.ScoreInfo.OnlineID} to {filename}");

                using (var outStream = File.Create(Path.Combine(path, filename)))
                    legacyEncoder.Encode(outStream);
            }
        }
    }
}
