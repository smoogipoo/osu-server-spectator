// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.Countdown;
using osu.Server.Spectator.Hubs;
using Xunit;

namespace osu.Server.Spectator.Tests.Multiplayer
{
    public class AutomaticForceStartTest : MultiplayerTest
    {
        [Fact]
        public async Task CountdownDoesNotStartWhileAllPlayersLoading()
        {
            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);
            await Hub.StartMatch();

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                Assert.False(usage.Item?.IsCountdownRunning);
                UserReceiver.Verify(r => r.MatchEvent(It.IsAny<CountdownChangedEvent>()), Times.Never);
            }
        }

        [Fact]
        public async Task CountdownDoesNotStartWhileAllPlayersLoaded()
        {
            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);
            await Hub.StartMatch();
            await Hub.ChangeState(MultiplayerUserState.Loaded);

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                Assert.False(usage.Item?.IsCountdownRunning);
                UserReceiver.Verify(r => r.MatchEvent(It.IsAny<CountdownChangedEvent>()), Times.Never);
            }
        }

        [Fact]
        public async Task CountdownStartsWhenOnePlayerReadyForGameplay()
        {
            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser2);

            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser);

            await Hub.StartMatch();
            await Hub.ChangeState(MultiplayerUserState.Loaded);

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                Assert.False(usage.Item?.IsCountdownRunning);
                UserReceiver.Verify(r => r.MatchEvent(It.IsAny<CountdownChangedEvent>()), Times.Never);
            }

            await Hub.ChangeState(MultiplayerUserState.ReadyForGameplay);
            await waitForCountingDown();

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                Assert.True(usage.Item?.IsCountdownRunning);
                UserReceiver.Verify(r => r.MatchEvent(It.IsAny<CountdownChangedEvent>()), Times.Once);
                User2Receiver.Verify(r => r.MatchEvent(It.IsAny<CountdownChangedEvent>()), Times.Once);
            }
        }

        [Fact]
        public async Task CountdownStopsWhenSingleReadyUserAborts()
        {
            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser2);

            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser);

            await Hub.StartMatch();
            await Hub.ChangeState(MultiplayerUserState.Loaded);
            await Hub.ChangeState(MultiplayerUserState.ReadyForGameplay);

            using (var usage = await Hub.GetRoom(ROOM_ID))
                Assert.True(usage.Item?.IsCountdownRunning);

            await Hub.AbortGameplay();

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                Assert.True(usage.Item?.IsCountdownStoppedOrCancelled);
                UserReceiver.Verify(r => r.GameplayStarted(), Times.Never);
            }
        }

        [Fact]
        public async Task GameplayStartsWhenNonReadyUserAborts()
        {
            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser2);

            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser);

            await Hub.StartMatch();
            await Hub.ChangeState(MultiplayerUserState.Loaded);
            await Hub.ChangeState(MultiplayerUserState.ReadyForGameplay);

            using (var usage = await Hub.GetRoom(ROOM_ID))
                Assert.True(usage.Item?.IsCountdownRunning);

            SetUserContext(ContextUser2);
            await Hub.AbortGameplay();

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                Assert.True(usage.Item?.IsCountdownStoppedOrCancelled);
                UserReceiver.Verify(r => r.GameplayStarted(), Times.Once);
            }
        }

        [Fact]
        public async Task GameplayStartsForLoadedUsersWhenCountdownEnds()
        {
            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser2);

            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser);

            // User 1 becomes ready for gameplay.
            await Hub.StartMatch();
            await Hub.ChangeState(MultiplayerUserState.Loaded);
            await Hub.ChangeState(MultiplayerUserState.ReadyForGameplay);

            // User 2 becomes loaded but isn't ready for gameplay.
            SetUserContext(ContextUser2);
            await Hub.ChangeState(MultiplayerUserState.Loaded);

            await finishCountdown();

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                Assert.True(usage.Item?.State == MultiplayerRoomState.Playing);
                UserReceiver.Verify(r => r.GameplayStarted(), Times.Once);
                User2Receiver.Verify(r => r.GameplayStarted(), Times.Once);
            }
        }

        [Fact]
        public async Task GameplayDoesNotStartForStillLoadingUsersWhenCountdownEnds()
        {
            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser2);

            await Hub.JoinRoom(ROOM_ID);
            await Hub.ChangeState(MultiplayerUserState.Ready);

            SetUserContext(ContextUser);

            // User 1 becomes ready for gameplay. User 2 remains loading.
            await Hub.StartMatch();
            await Hub.ChangeState(MultiplayerUserState.Loaded);
            await Hub.ChangeState(MultiplayerUserState.ReadyForGameplay);

            await finishCountdown();

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                var room = usage.Item;
                Debug.Assert(room != null);

                Assert.True(room.State == MultiplayerRoomState.Playing);

                UserReceiver.Verify(r => r.GameplayStarted(), Times.Once);
                UserReceiver.Verify(r => r.AbortGameplayLoad(), Times.Never);
                User2Receiver.Verify(r => r.GameplayStarted(), Times.Never);
                User2Receiver.Verify(r => r.AbortGameplayLoad(), Times.Once);

                Assert.Equal(MultiplayerUserState.Playing, room.Users.Single(u => u.UserID == USER_ID).State);
                Assert.Equal(MultiplayerUserState.Idle, room.Users.Single(u => u.UserID == USER_ID_2).State);
            }
        }

        private async Task finishCountdown()
        {
            ServerMultiplayerRoom? room;

            using (var usage = await Hub.GetRoom(ROOM_ID))
            {
                room = usage.Item;
                room?.SkipToEndOfCountdown();
            }

            Debug.Assert(room != null);

            int attempts = 200;
            while (attempts-- > 0 && room.IsCountdownRunning)
                Thread.Sleep(10);
        }

        private async Task waitForCountingDown()
        {
            ServerMultiplayerRoom? room;

            using (var usage = await Hub.GetRoom(ROOM_ID))
                room = usage.Item;

            Debug.Assert(room != null);

            int attempts = 200;
            while (attempts-- > 0 && room.Countdown == null)
                Thread.Sleep(10);

            Assert.NotNull(room.Countdown);
        }
    }
}
