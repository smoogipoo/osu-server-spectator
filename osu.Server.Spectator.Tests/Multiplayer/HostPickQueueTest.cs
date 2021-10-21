// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Game.Online.Multiplayer.Queueing;
using Xunit;

namespace osu.Server.Spectator.Tests.Multiplayer
{
    public class HostPickQueueTest : MultiplayerTest
    {
        [Fact]
        public async Task EnqueueingNewItemReplacesExisting()
        {
            await Hub.JoinRoom(ROOM_ID);

            await Hub.SendMatchRequest(new EnqueuePlaylistItemRequest
            {
                BeatmapID = 5,
                BeatmapChecksum = "checksum"
            });
        }
    }
}
