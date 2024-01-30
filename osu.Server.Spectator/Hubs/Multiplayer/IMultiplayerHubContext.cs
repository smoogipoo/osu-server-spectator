// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Game.Online.Multiplayer;
using osu.Game.Online.Rooms;
using osu.Server.Spectator.Entities;

namespace osu.Server.Spectator.Hubs.Multiplayer
{
    /// <summary>
    /// Allows communication with multiplayer clients from potentially outside of a direct <see cref="MultiplayerHub"/> context.
    /// </summary>
    public interface IMultiplayerHubContext
    {
        /// <summary>
        /// Notifies users in a room of an event.
        /// </summary>
        /// <remarks>
        /// This should be used for events which have no permanent effect on state.
        /// For operations which are intended to persist (and be visible to new users which join a room) use <see cref="NotifyMatchRoomStateChanged"/> or <see cref="NotifyMatchUserStateChanged"/> instead.
        /// </remarks>
        /// <param name="room">The room to send the event to.</param>
        /// <param name="e">The event.</param>
        ValueTask NotifyNewMatchEvent(ServerMultiplayerRoom room, MatchServerEvent e);

        /// <summary>
        /// Notify users in a room that the room's <see cref="MultiplayerRoom.MatchState"/> has been altered.
        /// </summary>
        /// <param name="room">The room whose state has changed.</param>
        ValueTask NotifyMatchRoomStateChanged(ServerMultiplayerRoom room);

        /// <summary>
        /// Notifies users in a room that a user's <see cref="MultiplayerRoomUser.MatchState"/> has been altered.
        /// </summary>
        /// <param name="room">The room to send the event to.</param>
        /// <param name="user">The user whose state has changed.</param>
        ValueTask NotifyMatchUserStateChanged(ServerMultiplayerRoom room, MultiplayerRoomUser user);

        /// <summary>
        /// Notifies users in a room that a playlist item has been added.
        /// </summary>
        /// <param name="room">The room to send the event to.</param>
        /// <param name="item">The added item.</param>
        ValueTask NotifyPlaylistItemAdded(ServerMultiplayerRoom room, MultiplayerPlaylistItem item);

        /// <summary>
        /// Notifies users in a room that a playlist item has been removed.
        /// </summary>
        /// <param name="room">The room to send the event to.</param>
        /// <param name="playlistItemId">The removed item.</param>
        ValueTask NotifyPlaylistItemRemoved(ServerMultiplayerRoom room, long playlistItemId);

        /// <summary>
        /// Notifies users in a room that a playlist item has been changed.
        /// </summary>
        /// <remarks>
        /// Adjusts user mod selections to ensure mod validity, and unreadies all users and stops the current countdown if the currently-selected playlist item was changed.
        /// </remarks>
        /// <param name="room">The room to send the event to.</param>
        /// <param name="item">The changed item.</param>
        /// <param name="beatmapChanged">Whether the beatmap changed.</param>
        ValueTask NotifyPlaylistItemChanged(ServerMultiplayerRoom room, MultiplayerPlaylistItem item, bool beatmapChanged);

        /// <summary>
        /// Notifies users in a room that the room's settings have changed.
        /// </summary>
        /// <remarks>
        /// Adjusts user mod selections to ensure mod validity, unreadies all users, and stops the current countdown.
        /// </remarks>
        /// <param name="room">The room to send the event to.</param>
        /// <param name="playlistItemChanged">Whether the current playlist item changed.</param>
        ValueTask NotifySettingsChanged(ServerMultiplayerRoom room, bool playlistItemChanged);

        /// <summary>
        /// Retrieves a <see cref="ServerMultiplayerRoom"/> usage.
        /// </summary>
        /// <param name="roomId">The ID of the room to retrieve.</param>
        ValueTask<ItemUsage<ServerMultiplayerRoom>> GetRoom(long roomId);
    }
}
