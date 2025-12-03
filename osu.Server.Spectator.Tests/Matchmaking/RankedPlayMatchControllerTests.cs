// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.MatchTypes.RankedPlay;
using osu.Game.Online.Rooms;
using osu.Server.Spectator.Database.Models;
using osu.Server.Spectator.Hubs.Multiplayer.Matchmaking;
using osu.Server.Spectator.Hubs.Multiplayer.Matchmaking.Queue;
using osu.Server.Spectator.Tests.Multiplayer;
using Xunit;

namespace osu.Server.Spectator.Tests.Matchmaking
{
    public class RankedPlayMatchControllerTests : MultiplayerTest, IAsyncLifetime
    {
        public RankedPlayMatchControllerTests()
        {
            AppSettings.MatchmakingRoomRounds = 2;
            AppSettings.MatchmakingRoomAllowSkip = true;

            Database.Setup(db => db.GetRealtimeRoomAsync(ROOM_ID))
                    .Callback<long>(roomId => InitialiseRoom(roomId, 20))
                    .ReturnsAsync(() => new multiplayer_room
                    {
                        type = database_match_type.ranked_play,
                        ends_at = DateTimeOffset.Now.AddMinutes(5),
                        user_id = int.Parse(Hub.Context.UserIdentifier!),
                    });

            Database.Setup(db => db.GetMatchmakingUserStatsAsync(It.IsAny<int>(), It.IsAny<uint>()))
                    .Returns<int, uint>((userId, poolId) => Task.FromResult<matchmaking_user_stats?>(new matchmaking_user_stats
                    {
                        user_id = (uint)userId,
                        pool_id = poolId
                    }));
        }

        public async Task InitializeAsync()
        {
            using (var room = await Rooms.GetForUse(ROOM_ID, true))
                room.Item = await MatchmakingQueueBackgroundService.InitialiseRoomAsync(ROOM_ID, HubContext, DatabaseFactory.Object, EventLogger, [USER_ID, USER_ID_2], 0);
        }

        [Fact]
        public async Task NormalRoomFlow()
        {
            Database.Setup(db => db.GetAllScoresForPlaylistItem(It.IsAny<long>())).Returns(() => Task.FromResult((IEnumerable<SoloScore>)
            [
                new SoloScore
                {
                    user_id = USER_ID,
                    total_score = 10
                },
                new SoloScore
                {
                    user_id = USER_ID_2,
                    total_score = 5
                }
            ]));

            await verifyStage(RankedPlayStage.WaitForJoin);

            // Join the first user.
            await Hub.JoinRoom(ROOM_ID);
            Receiver.Verify(u => u.RankedPlayCardRevealed(It.IsAny<RankedPlayCard>(), It.IsAny<MultiplayerPlaylistItem>()), Times.Never);
            UserReceiver.Verify(u => u.RankedPlayCardRevealed(It.IsAny<RankedPlayCard>(), It.IsAny<MultiplayerPlaylistItem>()), Times.Exactly(5));

            UserReceiver.Invocations.Clear();
            await verifyStage(RankedPlayStage.WaitForJoin);

            // Join the second user.
            SetUserContext(ContextUser2);
            await Hub.JoinRoom(ROOM_ID);
            Receiver.Verify(u => u.RankedPlayCardRevealed(It.IsAny<RankedPlayCard>(), It.IsAny<MultiplayerPlaylistItem>()), Times.Never);
            UserReceiver.Verify(u => u.RankedPlayCardRevealed(It.IsAny<RankedPlayCard>(), It.IsAny<MultiplayerPlaylistItem>()), Times.Never);
            User2Receiver.Verify(u => u.RankedPlayCardRevealed(It.IsAny<RankedPlayCard>(), It.IsAny<MultiplayerPlaylistItem>()), Times.Exactly(5));

            Receiver.Invocations.Clear();
            UserReceiver.Invocations.Clear();
            User2Receiver.Invocations.Clear();

            await verifyStage(RankedPlayStage.RoundWarmup);

            await gotoNextStage();
            await verifyStage(RankedPlayStage.CardDiscard);

            SetUserContext(ContextUser);

            Receiver.Invocations.Clear();
            await gotoNextStage();

            await verifyStage(RankedPlayStage.Ended);
        }

        private async Task verifyStage(RankedPlayStage stage)
        {
            using (var room = await Rooms.GetForUse(ROOM_ID))
                Assert.Equal(stage, ((RankedPlayRoomState)room.Item!.MatchState!).Stage);
        }

        private async Task gotoNextStage()
        {
            RankedPlayMatchController controller;

            using (var room = await Rooms.GetForUse(ROOM_ID))
            {
                Assert.NotNull(room.Item);
                controller = (RankedPlayMatchController)room.Item.Controller;
            }

            controller.SkipToNextStage(out Task countdownTask);
            await countdownTask;
        }

        private async Task gotoStage(RankedPlayStage stage)
        {
            while (true)
            {
                RankedPlayStage currentStage;

                using (var room = await Rooms.GetForUse(ROOM_ID))
                    currentStage = ((RankedPlayRoomState)room.Item!.MatchState!).Stage;

                if (currentStage == stage)
                    break;

                switch (currentStage)
                {
                    case RankedPlayStage.WaitForJoin:
                        await gotoNextStage();
                        break;

                    case RankedPlayStage.CardDiscard:
                        await gotoNextStage();
                        break;

                    case RankedPlayStage.CardSelect:
                        await gotoNextStage();
                        break;

                    case RankedPlayStage.FinishSelection:
                        SetUserContext(ContextUser);
                        await Hub.ChangeState(MultiplayerUserState.Ready);
                        await Hub.ChangeBeatmapAvailability(BeatmapAvailability.LocallyAvailable());
                        break;

                    case RankedPlayStage.GameplayWarmup:
                        await gotoNextStage();
                        break;

                    case RankedPlayStage.Gameplay:
                        SetUserContext(ContextUser);
                        await Hub.ChangeState(MultiplayerUserState.Loaded);
                        await Hub.ChangeState(MultiplayerUserState.ReadyForGameplay);
                        await Hub.AbortGameplay();
                        break;

                    case RankedPlayStage.Results:
                        await gotoNextStage();
                        break;

                    case RankedPlayStage.Ended:
                        await gotoNextStage();
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(stage));
                }
            }
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
