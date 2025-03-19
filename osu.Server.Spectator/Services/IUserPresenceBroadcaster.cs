// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Extensions.Hosting;
using osu.Game.Users;

namespace osu.Server.Spectator.Services
{
    public interface IUserPresenceBroadcaster : IHostedService
    {
        void BroadcastStatus(int userId, UserStatus status);
        void BroadcastActivity(int userId, UserActivity? activity);
    }
}
