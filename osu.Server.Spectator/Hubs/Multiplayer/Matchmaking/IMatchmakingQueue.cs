// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Server.Spectator.Hubs.Multiplayer.Matchmaking
{
    public interface IMatchmakingQueue
    {
        /// <summary>
        /// Whether a user is in the queue.
        /// </summary>
        /// <param name="identifier">A unique identifier for the user.</param>
        bool IsInQueue(string identifier);

        /// <summary>
        /// Adds a user to the queue.
        /// </summary>
        /// <param name="identifier">A unique identifier for the user.</param>
        /// <param name="rank">The user's rank.</param>
        /// <returns>Whether the user was added to the queue.</returns>
        bool AddToQueue(string identifier, int rank);

        /// <summary>
        /// Removes a user from the queue.
        /// </summary>
        /// <param name="identifier">A unique identifier for the user.</param>
        /// <returns>Whether the user was removed from the queue.</returns>
        bool RemoveFromQueue(string identifier);

        /// <summary>
        /// Performs a single update of the queue.
        /// </summary>
        /// <returns>An enumeration containing the sets of players (as their identifiers) for which the queue has been fulfilled.</returns>
        IEnumerable<string[]> Update();
    }
}
