// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.Queueing;
using osu.Server.Spectator.Database;
using osu.Server.Spectator.Database.Models;

namespace osu.Server.Spectator.Hubs.Queues
{
    public class MultiplayerKaraokeQueue : IMultiplayerQueue
    {
        public bool CanEnqueue(MultiplayerRoom room, int userId) => true;

        public Task ValidateSettings(MultiplayerRoomSettings newSettings, MultiplayerRoom room, IDatabaseAccess db)
        {
            // Server is authoritative over everything to do with the playlist.
            newSettings.PlaylistItemId = room.Settings.PlaylistItemId;
            newSettings.BeatmapID = room.Settings.BeatmapID;
            newSettings.BeatmapChecksum = room.Settings.BeatmapChecksum;
            newSettings.RulesetID = room.Settings.RulesetID;
            newSettings.AllowedMods = room.Settings.AllowedMods;
            newSettings.RequiredMods = room.Settings.RequiredMods;
            return Task.CompletedTask;
        }

        public Task<long> Enqueue(EnqueuePlaylistItemRequest request, MultiplayerRoom room, IDatabaseAccess db)
        {
            return db.AddPlaylistItemAsync(new multiplayer_playlist_item
            {
                room_id = room.RoomID,
                beatmap_id = request.BeatmapID,
                ruleset_id = (short)request.RulesetID,
                allowed_mods = JsonConvert.SerializeObject(request.AllowedMods),
                required_mods = JsonConvert.SerializeObject(request.RequiredMods)
            });
        }

        public async Task<long> Dequeue(MultiplayerRoom room, IDatabaseAccess db)
        {
            // Expire the current playlist item.
            var currentItem = await db.GetCurrentPlaylistItemAsync(room.RoomID);
            await db.ExpirePlaylistItemAsync(currentItem.id);

            // Re-add the item as a fresh entry.
            return (await db.GetCurrentPlaylistItemAsync(room.RoomID)).id;
        }
    }
}
