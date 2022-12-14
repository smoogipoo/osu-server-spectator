// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Scoring;
using osu.Server.QueueProcessor;

namespace osu.Server.Spectator
{
    public class ReplayQueueItem : QueueItem
    {
        public Score Score;

        public ReplayQueueItem(Score score)
        {
            Score = score;
        }
    }
}
