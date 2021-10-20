// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Online.Multiplayer.Queueing;

namespace osu.Server.Spectator.Database.Models
{
    [Serializable]
    // ReSharper disable once InconsistentNaming
    public enum database_queue_modes
    {
        host_pick,
        karaoke
    }

    public static class DatabaseQueueModeExtensions
    {
        public static QueueModes ToQueueMode(this database_queue_modes mode)
        {
            switch (mode)
            {
                default:
                case database_queue_modes.host_pick:
                    return QueueModes.HostPick;

                case database_queue_modes.karaoke:
                    return QueueModes.Karaoke;
            }
        }

        public static database_queue_modes ToDatabaseQueueMode(this QueueModes mode)
        {
            switch (mode)
            {
                default:
                case QueueModes.HostPick:
                    return database_queue_modes.host_pick;

                case QueueModes.Karaoke:
                    return database_queue_modes.karaoke;
            }
        }
    }
}
