// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.Queueing;

namespace osu.Server.Spectator.Hubs.Queues
{
    public class MultiplayerKaraokeQueue : IMultiplayerQueue
    {
        public bool CanEnqueue(int userId, MultiplayerRoom room)
        {
            throw new System.NotImplementedException();
        }

        public void Enqueue(EnqueuePlaylistItemRequest request)
        {
            throw new System.NotImplementedException();
        }

        public int SelectNextItem()
        {
            throw new System.NotImplementedException();
        }
    }
}
