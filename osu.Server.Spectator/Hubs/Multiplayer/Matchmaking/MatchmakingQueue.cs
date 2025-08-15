// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Server.Spectator.Hubs.Multiplayer.Matchmaking
{
    public class MatchmakingQueue : IMatchmakingQueue
    {
        /// <summary>
        /// The required number of players to be fulfilled.
        /// </summary>
        private readonly int roomSize;

        /// <summary>
        /// Lock for <see cref="queue"/>.
        /// </summary>
        private readonly SemaphoreSlim queueLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The queue.
        /// </summary>
        private readonly HashSet<QueueUser> queue = new HashSet<QueueUser>();

        /// <summary>
        /// Creates a new <see cref="MatchmakingQueue"/>.
        /// </summary>
        /// <param name="roomSize">The required number of players to be fulfilled.</param>
        public MatchmakingQueue(int roomSize)
        {
            this.roomSize = roomSize;
        }

        /// <summary>
        /// Retrieves a context for performing operations on the queue.
        /// </summary>
        public Task<MatchmakingQueueContext> GetContextAsync()
        {
            return MatchmakingQueueContext.Create(this);
        }

        bool IMatchmakingQueue.IsInQueue(string connectionId)
        {
            return queue.Contains(new QueueUser(connectionId));
        }

        bool IMatchmakingQueue.AddToQueue(string identifier, int rank)
        {
            return queue.Add(new QueueUser(identifier));
        }

        bool IMatchmakingQueue.RemoveFromQueue(string connectionId)
        {
            return queue.Remove(new QueueUser(connectionId));
        }

        IEnumerable<string[]> IMatchmakingQueue.Update()
        {
            // Todo: This lock may be a bit too global.
            if (queue.Count < roomSize)
                return [];

            // Order users by priority in-case they weren't picked in a previous epoch.
            QueueUser[] usersByPriority = queue.ToArray();
            Array.Sort(usersByPriority);

            // Increment priority of all existing users.
            foreach (var user in queue)
                user.Priority++;

            return [];
        }

        public class MatchmakingQueueContext : IMatchmakingQueue, IDisposable
        {
            private readonly IMatchmakingQueue queue;

            private MatchmakingQueueContext(IMatchmakingQueue queue)
            {
                this.queue = queue;
            }

            public static async Task<MatchmakingQueueContext> Create(MatchmakingQueue queue)
            {
                await queue.queueLock.WaitAsync(TimeSpan.FromSeconds(10));
                return new MatchmakingQueueContext(queue);
            }

            public bool IsInQueue(string connectionId) => queue.IsInQueue(connectionId);
            public bool AddToQueue(string identifier, int rank) => queue.AddToQueue(identifier, rank);
            public bool RemoveFromQueue(string connectionId) => queue.RemoveFromQueue(connectionId);
            public IEnumerable<string[]> Update() => queue.Update();

            public void Dispose() => ((MatchmakingQueue)queue).queueLock.Release();
        }

        private class QueueUser : IEquatable<QueueUser>, IComparer<QueueUser>
        {
            public int Priority { get; set; }

            public readonly string ConnectionId;

            public QueueUser(string connectionId)
            {
                ConnectionId = connectionId;
            }

            public int Compare(QueueUser? x, QueueUser? y)
            {
                ArgumentNullException.ThrowIfNull(x);
                ArgumentNullException.ThrowIfNull(y);

                // x appears earlier in the list if it has a greater priority than y.
                return y.Priority.CompareTo(x.Priority);
            }

            public bool Equals(QueueUser? other)
                => other != null && ConnectionId == other.ConnectionId;

            public override bool Equals(object? obj)
                => obj is QueueUser other && Equals(other);

            public override int GetHashCode() => ConnectionId.GetHashCode();
        }
    }
}
