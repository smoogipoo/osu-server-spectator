// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using StatsdClient;

namespace osu.Server.Spectator.Hubs.Multiplayer
{
    [Serializable]
    public class MultiplayerClientState : ClientState
    {
        private static int countUsersInRooms;

        // user_id -> pool_id
        public readonly Dictionary<int, int> OutgoingChallenges = new Dictionary<int, int>();

        public long? CurrentRoomID { get; private set; }

        [JsonConstructor]
        public MultiplayerClientState(in string connectionId, in int userId)
            : base(connectionId, userId)
        {
        }

        public void SetRoom(long roomId)
        {
            if (CurrentRoomID != null)
                throw new InvalidOperationException("User is already in a room.");

            CurrentRoomID = roomId;
            DogStatsd.Gauge($"{MultiplayerHub.STATSD_PREFIX}.users", Interlocked.Increment(ref countUsersInRooms));
        }

        public void ClearRoom()
        {
            if (CurrentRoomID == null)
                return;

            CurrentRoomID = null;
            DogStatsd.Gauge($"{MultiplayerHub.STATSD_PREFIX}.users", Interlocked.Decrement(ref countUsersInRooms));
        }
    }
}
