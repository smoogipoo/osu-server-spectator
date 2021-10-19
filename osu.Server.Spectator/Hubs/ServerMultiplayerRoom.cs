// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Server.Spectator.Hubs.Queues;

namespace osu.Server.Spectator.Hubs
{
    public class ServerMultiplayerRoom : MultiplayerRoom
    {
        private readonly IMultiplayerServerMatchCallbacks hubCallbacks;

        private MatchTypeImplementation matchTypeImplementation;

        public MatchTypeImplementation MatchTypeImplementation
        {
            get => matchTypeImplementation;
            set
            {
                if (matchTypeImplementation == value)
                    return;

                matchTypeImplementation = value;

                foreach (var u in Users)
                    matchTypeImplementation.HandleUserJoined(u);
            }
        }

        public IMultiplayerQueue QueueImplementation { get; set; }

        public ServerMultiplayerRoom(long roomId, IMultiplayerServerMatchCallbacks hubCallbacks)
            : base(roomId)
        {
            this.hubCallbacks = hubCallbacks;

            // just to ensure non-null.
            matchTypeImplementation = createTypeImplementation(MatchType.HeadToHead);
            QueueImplementation = createQueueImplementation(QueueingModes.Host);
        }

        public void ChangeMatchType(MatchType type) => MatchTypeImplementation = createTypeImplementation(type);

        public void ChangeQueue(QueueingModes mode) => QueueImplementation = createQueueImplementation(mode);

        public void AddUser(MultiplayerRoomUser user)
        {
            Users.Add(user);
            MatchTypeImplementation.HandleUserJoined(user);
        }

        public void RemoveUser(MultiplayerRoomUser user)
        {
            Users.Remove(user);
            MatchTypeImplementation.HandleUserLeft(user);
        }

        private MatchTypeImplementation createTypeImplementation(MatchType type)
        {
            switch (type)
            {
                case MatchType.TeamVersus:
                    return new TeamVersus(this, hubCallbacks);

                default:
                    return new HeadToHead(this, hubCallbacks);
            }
        }

        private IMultiplayerQueue createQueueImplementation(QueueingModes mode)
        {
            switch (mode)
            {
                case QueueingModes.Host:
                    return new MultiplayerHostPickQueue();

                case QueueingModes.Karaoke:
                    return new MultiplayerKaraokeQueue();
            }
        }
    }
}
