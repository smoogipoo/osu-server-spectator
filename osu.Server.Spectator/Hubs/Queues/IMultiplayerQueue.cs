// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.Queueing;

namespace osu.Server.Spectator.Hubs.Queues
{
    public interface IMultiplayerQueue
    {
        bool CanAdd(int userId, MultiplayerRoom room);

        void HandleAddRequest(AddPlaylistItemRequest request);

        int SelectNextItem();
    }
}
