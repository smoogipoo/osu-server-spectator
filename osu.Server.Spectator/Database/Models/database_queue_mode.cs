// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Online.Multiplayer;

namespace osu.Server.Spectator.Database.Models
{
    [Serializable]
    // ReSharper disable once InconsistentNaming
    public enum database_queue_mode
    {
        host_pick,
        karaoke
    }

    public static class DatabaseQueueModeExtensions
    {
        public static QueueModes ToQueueingMode(this database_queue_mode mode)
        {
            switch (mode)
            {
                default:
                case database_queue_mode.host_pick:
                    return QueueModes.HostPick;

                case database_queue_mode.karaoke:
                    return QueueModes.Karaoke;
            }
        }
    }
}
