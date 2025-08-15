// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace osu.Server.Spectator.Hubs.Multiplayer.Matchmaking
{
    public interface IMatchmakingQueueService : IHostedService
    {
        /// <summary>
        /// Whether the given user is in the matchmaking queue.
        /// </summary>
        /// <param name="connectionId">The user connection.</param>
        Task<bool> IsInQueueAsync(string connectionId);

        /// <summary>
        /// Adds a user to the matchmaking queue.
        /// </summary>
        /// <param name="connectionId">The user's connection.</param>
        /// <param name="userId">The user's ID.</param>
        Task AddToQueueAsync(string connectionId, int userId);

        /// <summary>
        /// Removes a user from the matchmaking queue.
        /// </summary>
        /// <param name="connectionId">The user's connection.</param>
        Task RemoveFromQueueAsync(string connectionId);
    }
}
