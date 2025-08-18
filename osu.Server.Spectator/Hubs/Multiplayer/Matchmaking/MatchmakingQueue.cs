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
            if (queue.Count < RoomSize)
                yield break;

            // Mapping of rank -> bucket.
            Dictionary<int, UserBucket> bucketsByRank = new Dictionary<int, UserBucket>();

            // Mapping of user -> buckets in which the user is present.
            Dictionary<QueueUser, List<UserBucket>> bucketsByUser = new Dictionary<QueueUser, List<UserBucket>>();

            // Add users in buckets formed by their expanded rank. Users may be added to multiple buckets.
            foreach (var user in queue)
            {
                int expansion = SearchExpansion(user.Rank);

                int minRank = (int)Math.Floor(Math.Max(0, user.Rank - expansion) / (double)SearchWidth) * SearchWidth;
                int maxRank = (int)Math.Ceiling(user.Rank + expansion / (double)SearchWidth) * SearchWidth;

                for (int rank = minRank; rank <= maxRank; rank += SearchWidth)
                {
                    if (!bucketsByRank.TryGetValue(rank, out UserBucket? bucket))
                        bucketsByRank[rank] = bucket = new UserBucket();
                    if (!bucketsByUser.TryGetValue(user, out List<UserBucket>? userBuckets))
                        bucketsByUser[user] = userBuckets = new List<UserBucket>();

                    bucket.Users.Add(user);
                    userBuckets.Add(bucket);
                }
            }

            // Sort buckets by their search iteration, bringing those containing users who've been waiting the longest to the front of the search.
            UserBucket?[] bucketsByPriority = bucketsByRank.Values.OrderByDescending(b => b.Users.Select(u => u.SearchIteration).DefaultIfEmpty(0).Max()).ToArray();

            // Attempt to fulfill the search for each bucket.
            foreach (var bucket in bucketsByPriority)
            {
                if (bucket == null)
                    continue;

                while (bucket.Users.Count >= RoomSize)
                {
                    // Sort users by their search iteration, bringing those who've been waiting the longest to the front of the search.
                    QueueUser[] usersByPriority = bucket.Users.OrderByDescending(u => u.SearchIteration).ToArray();
                    string[] userIdentifiers = new string[RoomSize];

                    // Collect users, removing them from their respective buckets and the queue as a whole.
                    for (int i = 0; i < RoomSize; i++)
                    {
                        QueueUser user = usersByPriority[i];

                        userIdentifiers[i] = user.Identifier;

                        foreach (var b in bucketsByUser[user])
                            b.Users.Remove(user);

                        queue.Remove(user);
                    }

                    yield return userIdentifiers;
                }
            }

            // Increment the search iteration for all remaining users.
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

        private class UserBucket
        {
            public readonly HashSet<QueueUser> Users = new HashSet<QueueUser>();
        }

        private class QueueUser : IEquatable<QueueUser>
        {
            /// <summary>
            /// The amount of search iterations this user has been waiting for.
            /// </summary>
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
        }
    }
}
