// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using osu.Game.Online.Metadata;
using osu.Game.Users;
using osu.Server.Spectator.Hubs.Metadata;

namespace osu.Server.Spectator.Services
{
    public class UserPresenceBroadcaster : BackgroundService, IUserPresenceBroadcaster
    {
        private readonly IHubContext<MetadataHub> metadataContext;

        private readonly ReaderWriterLockSlim updateLock = new ReaderWriterLockSlim();
        private readonly ConcurrentDictionary<int, (UserPresence from, UserPresence to)> pendingUpdates = new ConcurrentDictionary<int, (UserPresence, UserPresence)>();

        public UserPresenceBroadcaster(IHubContext<MetadataHub> metadataContext)
        {
            this.metadataContext = metadataContext;
        }

        public void BroadcastChange(int userId, UserPresence from, UserPresence to)
        {
            try
            {
                updateLock.EnterReadLock();

                from = pendingUpdates.GetValueOrDefault(userId, (from, to)).from;

                if (from.Equals(to))
                {
                    pendingUpdates.TryRemove(userId, out _);
                    return;
                }

                pendingUpdates[userId] = (from, to);
            }
            finally
            {
                updateLock.ExitReadLock();
            }
        }

        public Task Flush()
        {
            return broadcastUpdates(CancellationToken.None);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await broadcastUpdates(stoppingToken);
                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task broadcastUpdates(CancellationToken stoppingToken)
        {
            try
            {
                updateLock.EnterWriteLock();

                foreach ((int userId, (UserPresence from, UserPresence to)) in pendingUpdates)
                {
                    if (from.Equals(to))
                        continue;

                    await metadataContext.Clients.Group(MetadataHub.USER_STATUS_WATCHERS_GROUP)
                                         .SendCoreAsync(nameof(IMetadataClient.UserPresenceUpdated), [userId, new UserPresence { Status = to.Status }], stoppingToken);

                    await metadataContext.Clients.Group(MetadataHub.USER_ACTIVITY_WATCHERS_GROUP)
                                         .SendCoreAsync(nameof(IMetadataClient.UserPresenceUpdated), [userId, to], stoppingToken);
                }

                pendingUpdates.Clear();
            }
            finally
            {
                updateLock.ExitWriteLock();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            updateLock.Dispose();
        }
    }
}
