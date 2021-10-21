// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using osu.Game.Online.API;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.Queueing;
using osu.Server.Spectator.Database;

namespace osu.Server.Spectator.Hubs.Queues
{
    public class MultiplayerPresetQueue : IMultiplayerQueue
    {
        public async Task ValidateSettings(MultiplayerRoomSettings newSettings, MultiplayerRoom room, IDatabaseAccess db)
        {
            // User is authoritative over only the playlist ID, but it must be validated.
            var item = await db.GetPlaylistItemFromRoomAsync(room.RoomID, newSettings.PlaylistItemId);
            if (item == null)
                throw new InvalidStateException("Selected playlist item does not exist in the room.");

            // Server is authoritative over the contents of the playlist item.
            newSettings.BeatmapID = item.beatmap_id;
            newSettings.RulesetID = item.ruleset_id;
            newSettings.BeatmapChecksum = await db.GetBeatmapChecksumAsync(item.beatmap_id) ?? string.Empty;
            newSettings.RequiredMods = JsonConvert.DeserializeObject<APIMod[]>(item.required_mods ?? string.Empty) ?? Array.Empty<APIMod>();
            newSettings.AllowedMods = JsonConvert.DeserializeObject<APIMod[]>(item.allowed_mods ?? string.Empty) ?? Array.Empty<APIMod>();
        }

        public Task<long> Enqueue(EnqueuePlaylistItemRequest request, MultiplayerRoom room, IDatabaseAccess db)
        {
            throw new InvalidStateException("Cannot enqueue to a preset beatmap queue");
        }

        public Task<long> Dequeue(MultiplayerRoom room, IDatabaseAccess db)
        {
            throw new NotImplementedException();
        }
    }
}
