// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using osu.Game.Arcade;
using osu.Server.Spectator.Entities;
using osu.Server.Spectator.Extensions;

namespace osu.Server.Spectator.Hubs.Arcade
{
    public class ArcadeHub : StatefulUserHub<IArcadeClient, ArcadeClientState>, IArcadeServer
    {
        public ArcadeHub(ILoggerFactory loggerFactory, EntityStore<ArcadeClientState> userStates)
            : base(loggerFactory, userStates)
        {
        }

        public async Task Connect(ArcadeIdentity identity)
        {
            using (var state = await GetOrCreateLocalUserState())
                state.Item = new ArcadeClientState(Context.ConnectionId, Context.GetUserId(), identity);
            await Clients.All.UserConnected(Context.GetUserId(), identity);
        }

        public async Task Disconnect()
        {
            await UserStates.Destroy(Context.GetUserId());
            await Clients.All.UserDisconnected(Context.GetUserId());
        }
    }
}
