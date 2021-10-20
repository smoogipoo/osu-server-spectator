// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.Queueing;
using osu.Game.Online.Rooms;

namespace osu.Server.Spectator.Hubs.Queues
{
    public class MultiplayerHostPickQueue : IMultiplayerQueue
    {
        private PlaylistItem item;

        public bool CanAdd(int userId, MultiplayerRoom room) => userId == room.Host?.UserID;

        public void HandleAddRequest(AddPlaylistItemRequest request)
        {
            throw new System.NotImplementedException();
        }

        public int SelectNextItem()
        {
            throw new System.NotImplementedException();
        }
    }
}
