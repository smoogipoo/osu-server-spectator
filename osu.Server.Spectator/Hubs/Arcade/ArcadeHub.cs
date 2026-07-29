// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using osu.Game.Arcade;
using osu.Game.Online.Multiplayer;
using osu.Server.Spectator.Database;
using osu.Server.Spectator.Entities;
using osu.Server.Spectator.Extensions;
using osu.Server.Spectator.Hubs.Multiplayer;

namespace osu.Server.Spectator.Hubs.Arcade
{
    public class ArcadeHub : StatefulUserHub<IArcadeClient, ArcadeClientState>, IArcadeServer
    {
        private readonly IDatabaseFactory dbFactory;
        private readonly EntityStore<ServerMultiplayerRoom> roomStore;

        public ArcadeHub(ILoggerFactory loggerFactory, EntityStore<ArcadeClientState> userStates, IDatabaseFactory dbFactory,
                         EntityStore<ServerMultiplayerRoom> roomStore)
            : base(loggerFactory, userStates)
        {
            this.dbFactory = dbFactory;
            this.roomStore = roomStore;
        }

        public async Task<ArcadeUserStats[]> FetchLeaderboard()
        {
            using (var db = dbFactory.GetInstance())
                return await db.GetArcadeUserStatsAsync();
        }

        public Task<MultiplayerRoom[]> GetActiveRooms()
        {
            return Task.FromResult(roomStore.GetAllEntities().Select(e => e.Value.TakeSnapshot()).ToArray());
        }

        public async Task Connect(ArcadeIdentity identity)
        {
            using (var state = await GetOrCreateLocalUserState())
                state.Item = new ArcadeClientState(Context.ConnectionId, Context.GetUserId(), identity);
            await Clients.All.UserConnected(Context.GetUserId(), identity);
        }

        public async Task Disconnect()
        {
            // This is here because this hub is independent of multiplayer hub, and entities need to exist
            // for just a short while longer after players leave rooms.
            // This delay is imperceptible to players at the arcade.
            await Task.Delay(1000);

            await UserStates.Destroy(Context.GetUserId());
            await Clients.All.UserDisconnected(Context.GetUserId());
        }
    }
}
