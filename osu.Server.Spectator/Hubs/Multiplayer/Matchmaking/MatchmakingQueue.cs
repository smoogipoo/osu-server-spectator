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
        /// The required number of players for a search to be fulfilled.
        /// </summary>
        public int RoomSize { get; set; } = MatchmakingImplementation.MATCHMAKING_ROOM_SIZE;

        /// <summary>
        /// The initial search width users are bucketed into.
        /// </summary>
        public int SearchWidth { get; set; } = 1000;

        /// <summary>
        /// Given a search iteration, determines the amount to expand the search width at that iteration.
        /// </summary>
        public Func<int, int> SearchExpansion { get; set; } = it => (int)Math.Round(2000 * Math.Log(1 + 0.2 * it));

        /// <summary>
        /// Lock for <see cref="queue"/>.
        /// </summary>
        private readonly SemaphoreSlim queueLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The queue.
        /// </summary>
        private readonly HashSet<QueueUser> queue = new HashSet<QueueUser>();

        /// <summary>
        /// Retrieves a context for performing operations on the queue.
        /// </summary>
        public Task<MatchmakingQueueContext> GetContextAsync()
        {
            return MatchmakingQueueContext.Create(this);
        }

        bool IMatchmakingQueue.IsInQueue(string identifier)
        {
            return queue.Contains(new QueueUser(identifier));
        }

        bool IMatchmakingQueue.AddToQueue(string identifier, int rank)
        {
            return queue.Add(new QueueUser(identifier, rank));
        }

        bool IMatchmakingQueue.RemoveFromQueue(string identifier)
        {
            return queue.Remove(new QueueUser(identifier));
        }

        IEnumerable<string[]> IMatchmakingQueue.Update()
        {
            // Todo: This lock may be a bit too global.
            if (queue.Count < RoomSize)
                yield break;

            UserBucket?[] buckets = new UserBucket?[0];
            int minRank = int.MaxValue;
            int maxRank = int.MinValue;

            // Add users in buckets formed by their expanded rank.
            // A user with rank 10000 may be present in multiple buckets [ 8000, 9000, 10000, 11000, 12000 ].

            foreach (var user in queue)
            {
                int expansion = SearchExpansion(user.Rank);

                minRank = Math.Min(minRank, user.Rank - expansion);
                maxRank = Math.Max(maxRank, user.Rank + expansion);

                int minBucket = (int)Math.Floor((double)minRank / SearchWidth);
                int maxBucket = (int)Math.Ceiling((double)maxRank / SearchWidth);

                minBucket = Math.Max(0, minBucket);
                maxBucket = Math.Min(100, maxBucket);

                Array.Resize(ref buckets, maxBucket + 1);

                for (int b = minBucket; b <= maxBucket; b++)
                {
                    buckets[b] ??= new UserBucket();
                    buckets[b]!.Users.Add(user);
                    buckets[b]!.SearchIteration += user.SearchIteration;
                }
            }

            // Sort buckets by their users' aggregate search iteration.
            // This will bring users who've been waiting the longest to the front of the search.

            UserBucket?[] bucketsByPriority = buckets.ToArray();
            Array.Sort(bucketsByPriority);

            // Go through each bucket and attempt to fulfill the search.

            foreach (var bucket in bucketsByPriority)
            {
                if (bucket == null)
                    continue;

                while (bucket.Users.Count >= RoomSize)
                {
                    // Sort users by their search iteration.
                    // This will bring those who've been waiting the longest to the front of the search.

                    QueueUser[] usersByPriority = bucket.Users.ToArray();
                    Array.Sort(usersByPriority);

                    // Build the list of matching users.

                    string[] identifiers = new string[RoomSize];

                    for (int i = 0; i < RoomSize; i++)
                    {
                        identifiers[i] = usersByPriority[i].Identifier;
                        bucket.Users.Remove(usersByPriority[i]);
                        queue.Remove(usersByPriority[i]);
                    }

                    yield return identifiers;
                }
            }

            foreach (var user in queue)
                user.SearchIteration++;
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

            public bool IsInQueue(string identifier) => queue.IsInQueue(identifier);
            public bool AddToQueue(string identifier, int rank) => queue.AddToQueue(identifier, rank);
            public bool RemoveFromQueue(string identifier) => queue.RemoveFromQueue(identifier);
            public IEnumerable<string[]> Update() => queue.Update();

            public void Dispose() => ((MatchmakingQueue)queue).queueLock.Release();
        }

        private class UserBucket : IComparable<UserBucket>
        {
            public int SearchIteration { get; set; }

            public readonly HashSet<QueueUser> Users = new HashSet<QueueUser>();

            public int CompareTo(UserBucket? other)
            {
                ArgumentNullException.ThrowIfNull(other);

                // This appears earlier in the list if it has an older search iteration than the other.
                return other.SearchIteration.CompareTo(SearchIteration);
            }
        }

        private class QueueUser : IEquatable<QueueUser>, IComparable<QueueUser>
        {
            public int SearchIteration { get; set; }

            public readonly string Identifier;
            public readonly int Rank;

            public QueueUser(string identifier)
            {
                Identifier = identifier;
            }

            public QueueUser(string identifier, int rank)
            {
                Identifier = identifier;
                Rank = rank;
            }

            public bool Equals(QueueUser? other)
                => other != null && Identifier == other.Identifier;

            public override bool Equals(object? obj)
                => obj is QueueUser other && Equals(other);

            public override int GetHashCode() => Identifier.GetHashCode();

            public int CompareTo(QueueUser? other)
            {
                ArgumentNullException.ThrowIfNull(other);

                // This appears earlier in the list if it has an older search iteration than the other.
                return other.SearchIteration.CompareTo(SearchIteration);
            }
        }
    }
}
