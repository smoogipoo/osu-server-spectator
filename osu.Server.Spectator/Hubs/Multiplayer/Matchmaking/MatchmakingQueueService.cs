// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using osu.Game.Online.Matchmaking;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Server.Spectator.Database;
using osu.Server.Spectator.Database.Models;
using osu.Server.Spectator.Services;

namespace osu.Server.Spectator.Hubs.Multiplayer.Matchmaking
{
    public class MatchmakingQueueService : BackgroundService, IMatchmakingQueueService
    {
        private readonly MatchmakingQueue queue = new MatchmakingQueue(MatchmakingImplementation.MATCHMAKING_ROOM_SIZE);

        private readonly IHubContext<MultiplayerHub> hub;
        private readonly ISharedInterop sharedInterop;
        private readonly IDatabaseFactory databaseFactory;

        private MultiplayerPlaylistItem[]? playlistItems;

        public MatchmakingQueueService(IHubContext<MultiplayerHub> hub, ISharedInterop sharedInterop, IDatabaseFactory databaseFactory)
        {
            this.hub = hub;
            this.sharedInterop = sharedInterop;
            this.databaseFactory = databaseFactory;
        }

        public async Task<bool> IsInQueueAsync(string connectionId)
        {
            using (var context = await queue.GetContextAsync())
                return context.IsInQueue(connectionId);
        }

        public async Task AddToQueueAsync(string connectionId, int userId)
        {
            int rank;
            using (var db = databaseFactory.GetInstance())
                rank = (int)await db.GetUserPP(userId, 0);

            using (var context = await queue.GetContextAsync())
            {
                if (!context.AddToQueue(connectionId, rank))
                    return;
            }

            await hub.Clients.Client(connectionId).SendAsync(nameof(IMultiplayerClient.MatchmakingQueueStatusChanged), new MatchmakingQueueStatus.InQueue
            {
                RoomSize = MatchmakingImplementation.MATCHMAKING_ROOM_SIZE,
                PlayerCount = 1
            });
        }

        public async Task RemoveFromQueueAsync(string connectionId)
        {
            using (var context = await queue.GetContextAsync())
            {
                if (!context.RemoveFromQueue(connectionId))
                    return;
            }

            await hub.Clients.Client(connectionId).SendAsync(nameof(IMultiplayerClient.MatchmakingQueueStatusChanged), null);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var context = await queue.GetContextAsync())
                {
                    foreach (string[] playerList in context.Update())
                        await makeRoomAsync(playerList);
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task makeRoomAsync(string[] connectionIds)
        {
            long roomId = await sharedInterop.CreateRoomAsync(AppSettings.BanchoBotUserId, new MultiplayerRoom(0)
            {
                Settings = { MatchType = MatchType.Matchmaking },
                Playlist = await createPlaylistItems()
            });

            await hub.Clients.Clients(connectionIds).SendAsync(nameof(IMultiplayerClient.MatchmakingQueueStatusChanged), new MatchmakingQueueStatus.FoundMatch
            {
                RoomId = roomId
            });
        }

        private async Task<MultiplayerPlaylistItem[]> createPlaylistItems()
        {
            if (playlistItems == null)
            {
                using (var db = databaseFactory.GetInstance())
                {
                    database_beatmap[] beatmaps = await db.GetBeatmapsAsync(MatchmakingImplementation.BEATMAP_IDS);
                    playlistItems = beatmaps.Select(b => new MultiplayerPlaylistItem
                    {
                        BeatmapID = b.beatmap_id,
                        BeatmapChecksum = b.checksum!,
                        StarRating = b.difficultyrating
                    }).ToArray();
                }
            }

            // Per-room isolation of playlist items.
            return playlistItems.Select(p => p.Clone()).ToArray();
        }
    }
}
