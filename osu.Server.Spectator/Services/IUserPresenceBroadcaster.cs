// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using osu.Game.Users;

namespace osu.Server.Spectator.Services
{
    public interface IUserPresenceBroadcaster : IHostedService
    {
        /// <summary>
        /// Broadcasts a change in a user's presence.
        /// </summary>
        /// <param name="userId">The user whose presence changed.</param>
        /// <param name="from">The user's old presence.</param>
        /// <param name="to">The user's new presence.</param>
        void BroadcastChange(int userId, UserPresence from, UserPresence to);

        /// <summary>
        /// Flushes any pending changes, immediately broadcasting them to clients.
        /// </summary>
        Task Flush();
    }
}
