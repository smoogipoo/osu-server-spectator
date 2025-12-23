// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.MatchTypes.RankedPlay;
using osu.Game.Online.RankedPlay;
using osu.Server.Spectator.Database;

namespace osu.Server.Spectator.Hubs.Multiplayer.Matchmaking.RankedPlay
{
    public abstract class RankedPlayStageImplementation
    {
        protected abstract RankedPlayStage Stage { get; }
        protected abstract TimeSpan Duration { get; }

        protected ServerMultiplayerRoom Room => Controller.Room;
        protected IMultiplayerHubContext Hub => Controller.Hub;
        protected RankedPlayRoomState State => Controller.State;
        protected IDatabaseFactory DbFactory => Controller.DbFactory;
        protected MultiplayerEventLogger EventLogger => Controller.EventLogger;

        protected readonly RankedPlayMatchController Controller;

        protected RankedPlayStageImplementation(RankedPlayMatchController controller)
        {
            Controller = controller;
        }

        public async Task Enter()
        {
            State.Stage = Stage;
            await Hub.NotifyMatchRoomStateChanged(Room);

            await Begin();
            await FinishWithCountdown(Duration);
        }

        protected async Task FinishWithCountdown(TimeSpan duration)
        {
            await Room.StartCountdown(new RankedPlayStageCountdown
            {
                Stage = Stage,
                TimeRemaining = duration
            }, async _ => await Finish());
        }

        protected abstract Task Begin();

        protected abstract Task Finish();

        public virtual Task HandleUserJoined(MultiplayerRoomUser user)
        {
            return Task.CompletedTask;
        }

        public virtual Task HandleUserStateChanged(MultiplayerRoomUser user)
        {
            return Task.CompletedTask;
        }

        public virtual Task HandleGameplayCompleted()
        {
            return Task.CompletedTask;
        }

        public virtual Task HandleDiscardCards(MultiplayerRoomUser user, RankedPlayCardItem[] cards)
        {
            return Task.CompletedTask;
        }

        public virtual Task HandlePlayCard(MultiplayerRoomUser user, RankedPlayCardItem card)
        {
            return Task.CompletedTask;
        }
    }
}
