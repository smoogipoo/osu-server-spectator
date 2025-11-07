// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using osu.Game.Online.Matchmaking;
using osu.Game.Online.Multiplayer;
using osu.Server.Spectator.Database.Models;
using osu.Server.Spectator.Extensions;
using osu.Server.Spectator.Hubs.Multiplayer.Matchmaking;
using osu.Server.Spectator.Hubs.Multiplayer.Matchmaking.Queue;

namespace osu.Server.Spectator.Hubs.Multiplayer
{
    public partial class MultiplayerHub : IMatchmakingServer
    {
        public async Task<MatchmakingPool[]> GetMatchmakingPools()
        {
            using (var db = databaseFactory.GetInstance())
                return (await db.GetActiveMatchmakingPoolsAsync()).Select(p => p.ToMatchmakingPool()).ToArray();
        }

        public async Task MatchmakingJoinLobby()
        {
            using (var userUsage = await GetOrCreateLocalUserState())
                await matchmakingQueueService.AddToLobbyAsync(userUsage.Item!);
        }

        public async Task MatchmakingLeaveLobby()
        {
            using (var userUsage = await GetOrCreateLocalUserState())
                await matchmakingQueueService.RemoveFromLobbyAsync(userUsage.Item!);
        }

        public async Task MatchmakingJoinQueue(int poolId)
        {
            using (var userUsage = await GetOrCreateLocalUserState())
                await matchmakingQueueService.AddToQueueAsync(userUsage.Item!, poolId);
        }

        public async Task MatchmakingLeaveQueue()
        {
            using (var userUsage = await GetOrCreateLocalUserState())
                await matchmakingQueueService.RemoveFromQueueAsync(userUsage.Item!);
        }

        public async Task MatchmakingAcceptInvitation()
        {
            using (var userUsage = await GetOrCreateLocalUserState())
                await matchmakingQueueService.AcceptInvitationAsync(userUsage.Item!);
        }

        public async Task MatchmakingDeclineInvitation()
        {
            using (var userUsage = await GetOrCreateLocalUserState())
                await matchmakingQueueService.DeclineInvitationAsync(userUsage.Item!);
        }

        public async Task MatchmakingToggleSelection(long playlistItemId)
        {
            using (var userUsage = await GetOrCreateLocalUserState())
            using (var roomUsage = await getLocalUserRoom(userUsage.Item!))
            {
                var room = roomUsage.Item;
                if (room == null)
                    throw new InvalidOperationException("Attempted to operate on a null room");

                var user = room.Users.FirstOrDefault(u => u.UserID == Context.GetUserId());
                if (user == null)
                    throw new InvalidOperationException("Local user was not found in the expected room");

                await ((MatchmakingMatchController)room.Controller).ToggleSelectionAsync(user, playlistItemId);
            }
        }

        public async Task MatchmakingSkipToNextStage()
        {
            using (var userUsage = await GetOrCreateLocalUserState())
            using (var roomUsage = await getLocalUserRoom(userUsage.Item!))
            {
                var room = roomUsage.Item;
                if (room == null)
                    throw new InvalidOperationException("Attempted to operate on a null room");

                var user = room.Users.FirstOrDefault(u => u.UserID == Context.GetUserId());
                if (user == null)
                    throw new InvalidOperationException("Local user was not found in the expected room");

                ((MatchmakingMatchController)room.Controller).SkipToNextStage(out _);
            }
        }

        public async Task MatchmakingIssueChallenge(int poolId, int userId)
        {
            using (var userUsage = await GetOrCreateLocalUserState())
                userUsage.Item!.PendingChallenges[userId] = poolId;

            await Clients.User(userId.ToString()).MatchmakingChallengeIssued(poolId, Context.GetUserId());
        }

        public async Task MatchmakingAcceptChallenge(int userId)
        {
            using (var localUser = await GetOrCreateLocalUserState())
            using (var otherUser = await GetStateFromUser(userId))
            {
                if (!otherUser.Item!.PendingChallenges.Remove(Context.GetUserId(), out int poolId))
                    throw new InvalidStateException("There is no challenge request from the user.");

                // Remove both players from the quick play matchmaking queue.
                await matchmakingQueueService.RemoveFromQueueAsync(localUser.Item!);
                await matchmakingQueueService.RemoveFromQueueAsync(otherUser.Item!);

                using (var db = databaseFactory.GetInstance())
                {
                    matchmaking_pool pool = await db.GetMatchmakingPoolAsync(poolId) ?? throw new InvalidStateException($"Pool not found: {poolId}");

                    MatchmakingQueueUser localMatchmakingUser = await matchmakingQueueService.CreateUserAsync(pool, localUser.Item!);
                    MatchmakingQueueUser otherMatchmakingUser = await matchmakingQueueService.CreateUserAsync(pool, otherUser.Item!);

                    (long roomId, string password) = await matchmakingQueueService.CreateRoomAsync(pool, [localMatchmakingUser, otherMatchmakingUser]);
                    await Clients.Clients(localUser.Item!.ConnectionId, otherUser.Item!.ConnectionId).MatchmakingRoomReady(roomId, password);
                }
            }
        }

        public async Task MatchmakingDeclineChallenge(int userId)
        {
            using (var challengerUsage = await GetStateFromUser(userId))
            {
                if (!challengerUsage.Item!.PendingChallenges.Remove(Context.GetUserId()))
                    throw new InvalidStateException("There is no challenge request from the user.");
            }

            await Clients.User(userId.ToString()).MatchmakingChallengeDeclined(Context.GetUserId());
        }
    }
}
