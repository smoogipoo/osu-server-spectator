// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using osu.Game.Online;
using osu.Game.Online.Metadata;
using osu.Game.Users;
using osu.Server.Spectator.Database;
using osu.Server.Spectator.Database.Models;
using osu.Server.Spectator.Entities;
using osu.Server.Spectator.Extensions;
using osu.Server.Spectator.Hubs.Spectator;
using osu.Server.Spectator.Services;

namespace osu.Server.Spectator.Hubs.Metadata
{
    public class MetadataHub : StatefulUserHub<IMetadataClient, MetadataClientState>, IMetadataServer
    {
        private readonly IMemoryCache cache;
        private readonly IDatabaseFactory databaseFactory;
        private readonly IDailyChallengeUpdater dailyChallengeUpdater;
        private readonly IScoreProcessedSubscriber scoreProcessedSubscriber;
        private readonly IUserPresenceBroadcaster presenceBroadcaster;

        internal const string USER_STATUS_WATCHERS_GROUP = "metadata:online-status-watchers";

        internal const string USER_ACTIVITY_WATCHERS_GROUP = "metadata:online-activity-watchers";

        internal static string FRIEND_PRESENCE_WATCHERS_GROUP(int userId) => $"metadata:online-presence-watchers:{userId}";

        internal static string MultiplayerRoomWatchersGroup(long roomId) => $"metadata:multiplayer-room-watchers:{roomId}";

        public MetadataHub(
            ILoggerFactory loggerFactory,
            IMemoryCache cache,
            EntityStore<MetadataClientState> userStates,
            IDatabaseFactory databaseFactory,
            IDailyChallengeUpdater dailyChallengeUpdater,
            IScoreProcessedSubscriber scoreProcessedSubscriber,
            IUserPresenceBroadcaster presenceBroadcaster)
            : base(loggerFactory, userStates)
        {
            this.cache = cache;
            this.databaseFactory = databaseFactory;
            this.dailyChallengeUpdater = dailyChallengeUpdater;
            this.scoreProcessedSubscriber = scoreProcessedSubscriber;
            this.presenceBroadcaster = presenceBroadcaster;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            using (var usage = await GetOrCreateLocalUserState())
            {
                string? versionHash = null;

                if (Context.GetHttpContext()?.Request.Headers.TryGetValue(HubClientConnector.VERSION_HASH_HEADER, out StringValues headerValue) == true)
                {
                    versionHash = headerValue;

                    // The token is 82 chars long, and the clientHash is the first 32 of those.
                    // See: https://github.com/ppy/osu-web/blob/7be19a0fe0c9fa2f686e4bb686dbc8e9bf7bcf84/app/Libraries/ClientCheck.php#L92
                    if (versionHash?.Length >= 82)
                        versionHash = versionHash.Substring(versionHash.Length - 82, 32);
                }

                usage.Item = new MetadataClientState(Context.ConnectionId, Context.GetUserId(), versionHash);

                await logLogin(usage);
            }

            await Clients.Caller.DailyChallengeUpdated(dailyChallengeUpdater.Current);

            foreach ((_, MetadataClientState state) in GetAllStates())
            {
                await Groups.AddToGroupAsync(state.ConnectionId, USER_STATUS_WATCHERS_GROUP);

                UserPresence presence = state.GetPresence();

                if (presence.ShouldBroadcast())
                {
                    // Only broadcast status at this point..
                    await Clients.Caller.UserPresenceUpdated(state.UserId, state.GetStatus());
                }
            }
        }

        private async Task logLogin(ItemUsage<MetadataClientState> usage)
        {
            string? userIp = Context.GetHttpContext()?.Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwardedForIp) == true
                // header may contain multiple IPs by spec, first is usually what we care for.
                ? forwardedForIp.ToString().Split(',').First()
                // fallback to getting the raw IP.
                : Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();

            using (var db = databaseFactory.GetInstance())
                await db.AddLoginForUserAsync(usage.Item!.UserId, userIp);
        }

        public async Task<BeatmapUpdates> GetChangesSince(int queueId)
        {
            using (var db = databaseFactory.GetInstance())
                return await db.GetUpdatedBeatmapSets(queueId);
        }

        public async Task BeginWatchingUserPresence()
        {
            foreach ((_, MetadataClientState state) in GetAllStates())
            {
                UserPresence presence = state.GetPresence();
                if (presence.ShouldBroadcast())
                    await Clients.Caller.UserPresenceUpdated(state.UserId, presence);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, USER_STATUS_WATCHERS_GROUP);
            await Groups.AddToGroupAsync(Context.ConnectionId, USER_ACTIVITY_WATCHERS_GROUP);
        }

        public async Task EndWatchingUserPresence()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, USER_ACTIVITY_WATCHERS_GROUP);
            await Groups.AddToGroupAsync(Context.ConnectionId, USER_STATUS_WATCHERS_GROUP);
        }

        public async Task UpdateActivity(UserActivity? activity)
        {
            using (var usage = await GetOrCreateLocalUserState())
            {
                Debug.Assert(usage.Item != null);

                if (EqualityComparer<UserActivity>.Default.Equals(usage.Item.UserActivity, activity))
                    return;

                UserPresence oldPresence = usage.Item.GetPresence();
                usage.Item.UserActivity = activity;
                UserPresence newPresence = usage.Item.GetPresence();

                if (newPresence.ShouldBroadcast())
                    presenceBroadcaster.BroadcastChange(usage.Item.UserId, oldPresence, newPresence);
            }
        }

        public async Task UpdateStatus(UserStatus status)
        {
            using (var usage = await GetOrCreateLocalUserState())
            {
                Debug.Assert(usage.Item != null);

                if (usage.Item.UserStatus == status)
                    return;

                UserPresence oldPresence = usage.Item.GetPresence();
                usage.Item.UserStatus = status;
                UserPresence newPresence = usage.Item.GetPresence();

                // Always broadcast status so that the offline state is indistinguishable from being disconnected.
                presenceBroadcaster.BroadcastChange(usage.Item.UserId, oldPresence, newPresence);
            }
        }

        private static readonly object update_stats_lock = new object();

        public async Task<MultiplayerPlaylistItemStats[]> BeginWatchingMultiplayerRoom(long id)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, MultiplayerRoomWatchersGroup(id));
            await scoreProcessedSubscriber.RegisterForMultiplayerRoomAsync(Context.GetUserId(), id);

            using var db = databaseFactory.GetInstance();

            MultiplayerRoomStats stats = (await cache.GetOrCreateAsync<MultiplayerRoomStats>(id.ToString(), e =>
            {
                e.SlidingExpiration = TimeSpan.FromHours(24);
                return Task.FromResult(new MultiplayerRoomStats { RoomID = id });
            }))!;

            await updateMultiplayerRoomStatsAsync(db, stats);

            // Outside of locking so may be mid-update, but that's fine we don't need perfectly accurate for client-side.
            return stats.PlaylistItemStats.Values.ToArray();
        }

        private async Task updateMultiplayerRoomStatsAsync(IDatabaseAccess db, MultiplayerRoomStats stats)
        {
            long[] playlistItemIds = (await db.GetAllPlaylistItemsAsync(stats.RoomID)).Select(item => item.id).ToArray();

            for (int i = 0; i < playlistItemIds.Length; ++i)
            {
                long itemId = playlistItemIds[i];

                if (!stats.PlaylistItemStats.TryGetValue(itemId, out var itemStats))
                    stats.PlaylistItemStats[itemId] = itemStats = new MultiplayerPlaylistItemStats { PlaylistItemID = itemId, };

                ulong lastProcessed = itemStats.LastProcessedScoreID;

                SoloScore[] scores = (await db.GetPassingScoresForPlaylistItem(itemId, itemStats.LastProcessedScoreID)).ToArray();

                if (scores.Length == 0)
                    return;

                // Lock globally for simplicity.
                // If it ever becomes an issue we can move to per-item locking or something more complex.
                lock (update_stats_lock)
                {
                    // check whether last id has changed since database query completed. if it did, this means another run would have updated the stats.
                    // for simplicity, just skip the update and wait for the next.
                    if (lastProcessed == itemStats.LastProcessedScoreID)
                    {
                        Dictionary<int, long> totals = scores
                                                       .Select(s => s.total_score)
                                                       .GroupBy(score => (int)Math.Clamp(Math.Floor((float)score / 100000), 0, MultiplayerPlaylistItemStats.TOTAL_SCORE_DISTRIBUTION_BINS - 1))
                                                       .OrderBy(grp => grp.Key)
                                                       .ToDictionary(grp => grp.Key, grp => grp.LongCount());

                        itemStats.CumulativeScore += scores.Sum(s => s.total_score);
                        for (int j = 0; j < MultiplayerPlaylistItemStats.TOTAL_SCORE_DISTRIBUTION_BINS; j++)
                            itemStats.TotalScoreDistribution[j] += totals.GetValueOrDefault(j);
                        itemStats.LastProcessedScoreID = scores.Max(s => s.id);
                    }
                }
            }
        }

        public async Task EndWatchingMultiplayerRoom(long id)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, MultiplayerRoomWatchersGroup(id));
            await scoreProcessedSubscriber.UnregisterFromMultiplayerRoomAsync(Context.GetUserId(), id);
        }

        protected override async Task CleanUpState(MetadataClientState state)
        {
            await base.CleanUpState(state);

            UserPresence oldPresence = state.GetPresence();
            state.UserStatus = UserStatus.Offline;
            state.UserActivity = null;
            UserPresence newPresence = state.GetPresence();

            // Broadcast an offline state if we were previously broadcasting an online state.
            if (oldPresence.ShouldBroadcast())
                presenceBroadcaster.BroadcastChange(state.UserId, oldPresence, newPresence);

            await scoreProcessedSubscriber.UnregisterFromAllMultiplayerRoomsAsync(state.UserId);
        }
    }
}
