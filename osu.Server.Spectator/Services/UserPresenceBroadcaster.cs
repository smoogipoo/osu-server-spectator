// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<int, UserStatus> pendingStatusUpdates = new ConcurrentDictionary<int, UserStatus>();
        private readonly ConcurrentDictionary<int, UserActivity?> pendingActivityUpdates = new ConcurrentDictionary<int, UserActivity?>();

        public UserPresenceBroadcaster(IHubContext<MetadataHub> metadataContext)
        {
            this.metadataContext = metadataContext;
        }

        public void BroadcastStatus(int userId, UserStatus status)
        {
            updateLock.EnterReadLock();
            {
                pendingStatusUpdates[userId] = status;
                if (status == UserStatus.Offline)
                    pendingActivityUpdates.TryRemove(userId, out _);
            }
            updateLock.ExitReadLock();
        }

        public void BroadcastActivity(int userId, UserActivity? activity)
        {
            updateLock.EnterReadLock();
            {
                pendingActivityUpdates[userId] = activity;
            }
            updateLock.ExitReadLock();
        }

        public Task ExecuteImmediately()
        {
            return ExecuteAsync(CancellationToken.None);
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
            updateLock.EnterWriteLock();
            {
                foreach ((int userId, UserStatus status) in pendingStatusUpdates)
                    await metadataContext.Clients.All.SendCoreAsync(nameof(IMetadataClient.UserStatusUpdated), [userId, status], stoppingToken);

                foreach ((int userId, UserActivity? activity) in pendingActivityUpdates)
                    await metadataContext.Clients.Group(MetadataHub.USER_ACTIVITY_WATCHERS_GROUP).SendCoreAsync(nameof(IMetadataClient.UserActivityUpdated), [userId, activity], stoppingToken);

                pendingStatusUpdates.Clear();
                pendingActivityUpdates.Clear();
            }
            updateLock.ExitWriteLock();
        }

        public override void Dispose()
        {
            base.Dispose();
            updateLock.Dispose();
        }
    }
}
