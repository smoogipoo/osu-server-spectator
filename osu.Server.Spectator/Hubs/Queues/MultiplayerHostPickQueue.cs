// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.Queueing;
using osu.Server.Spectator.Database;

namespace osu.Server.Spectator.Hubs.Queues
{
    public class MultiplayerHostPickQueue : IMultiplayerQueue
    {
        public Task ValidateSettings(MultiplayerRoomSettings newSettings, MultiplayerRoom room, IDatabaseAccess db)
        {
            // Server is authoritative only over the playlist ID.
            newSettings.PlaylistItemId = room.Settings.PlaylistItemId;
            return Task.CompletedTask;
        }

        public Task<long> Enqueue(EnqueuePlaylistItemRequest request, MultiplayerRoom room, IDatabaseAccess db)
        {
            throw new InvalidStateException("Cannot enqueue to a host-pick beatmap queue");
        }

        public async Task<long> Dequeue(MultiplayerRoom room, IDatabaseAccess db)
        {
            // Expire the current playlist item.
            var currentItem = await db.GetCurrentPlaylistItemAsync(room.RoomID);
            await db.ExpirePlaylistItemAsync(currentItem.id);

            // Re-add the item as a fresh entry.
            return await db.AddPlaylistItemAsync(currentItem);
        }
    }
}
