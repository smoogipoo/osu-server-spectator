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
        private readonly ArcadeIdentityStore arcadeUsers;

        public ArcadeHub(ILoggerFactory loggerFactory, EntityStore<ArcadeClientState> userStates, ArcadeIdentityStore arcadeUsers)
            : base(loggerFactory, userStates)
        {
            this.arcadeUsers = arcadeUsers;
        }

        public Task Connect(ArcadeIdentity identity)
        {
            arcadeUsers.Add(Context.GetUserId(), identity);
            return Task.CompletedTask;
        }

        public Task Disconnect()
        {
            arcadeUsers.Remove(Context.GetUserId());
            return Task.CompletedTask;
        }
    }
}
