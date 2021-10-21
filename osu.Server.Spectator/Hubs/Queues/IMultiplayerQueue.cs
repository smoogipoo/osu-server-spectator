// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Multiplayer.Queueing;
using osu.Server.Spectator.Database;

namespace osu.Server.Spectator.Hubs.Queues
{
    public interface IMultiplayerQueue
    {
        /// <summary>
        /// Validates the requested <see cref="MultiplayerRoomSettings"/> against the <see cref="MultiplayerRoom"/>.
        /// </summary>
        /// <param name="newSettings">The settings to validate.</param>
        /// <param name="room">The <see cref="MultiplayerRoom"/> to validate the settings against.</param>
        /// <param name="db">The database.</param>
        Task ValidateSettings(MultiplayerRoomSettings newSettings, MultiplayerRoom room, IDatabaseAccess db);

        /// <summary>
        /// Enqueues a new playlist item to the room.
        /// </summary>
        /// <param name="request">The playlist item to enqueue.</param>
        /// <param name="room">The room to enqueue against.</param>
        /// <param name="db">The database.</param>
        /// <returns>The newly-enqueued item ID.</returns>
        Task<long> Enqueue(EnqueuePlaylistItemRequest request, MultiplayerRoom room, IDatabaseAccess db);

        /// <summary>
        /// Dequeues the current playlist item from the room, returning the next item's ID.
        /// </summary>
        /// <param name="room">The room to dequeue from.</param>
        /// <param name="db">The database.</param>
        /// <returns>The next playlist item ID.</returns>
        Task<long> Dequeue(MultiplayerRoom room, IDatabaseAccess db);
    }
}
