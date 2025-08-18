// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Server.Spectator.Hubs.Multiplayer.Matchmaking;
using Xunit;

namespace osu.Server.Spectator.Tests.Matchmaking
{
    public class MatchmakingQueueTest
    {
        private readonly MatchmakingQueue queue = new MatchmakingQueue();

        /// <summary>
        /// An empty queue updates correctly.
        /// </summary>
        [Fact]
        public void EmptyQueue()
        {
            string[][] sets = queue.Update().ToArray();
            Assert.Equal(0, sets.Length);
        }

        /// <summary>
        /// A single-player room is fulfilled immediately.
        /// </summary>
        [Fact]
        public void SinglePlayerRoomSize()
        {
            queue.RoomSize = 1;

            queue.AddToQueue("1", 0);
            string[][] sets = queue.Update().ToArray();

            Assert.Equal(1, sets.Length);
            Assert.Equal("1", sets[0][0]);
            Assert.False(queue.IsInQueue("1"));
        }

        /// <summary>
        /// Two players at the same rank get matched immediately.
        /// </summary>
        [Fact]
        public void TwoPlayersSameRank()
        {
            queue.RoomSize = 2;

            queue.AddToQueue("1", 5000);
            queue.AddToQueue("2", 5000);
            string[][] sets = queue.Update().ToArray();

            Assert.Equal(1, sets.Length);
            Assert.Equal("1", sets[0][0]);
            Assert.Equal("2", sets[0][1]);
            Assert.False(queue.IsInQueue("1"));
            Assert.False(queue.IsInQueue("2"));
        }

        /// <summary>
        /// Two players at a different eventually get matched up.
        /// </summary>
        [Fact]
        public void TwoPlayersDifferentRank()
        {
            queue.RoomSize = 2;
            queue.SearchExpansion = it => it * 1000;
            queue.SearchWidth = 1000;

            queue.AddToQueue("1", 5000);
            queue.AddToQueue("2", 0);

            // It should take 4 iterations for the search to be fulfilled.

            string[][] sets = queue.Update().ToArray();
            Assert.Equal(0, sets.Length);

            sets = queue.Update().ToArray();
            Assert.Equal(0, sets.Length);

            sets = queue.Update().ToArray();
            Assert.Equal(0, sets.Length);

            sets = queue.Update().ToArray();
            Assert.Equal(1, sets.Length);
            Assert.Equal("1", sets[0][0]);
            Assert.Equal("2", sets[0][1]);
            Assert.False(queue.IsInQueue("1"));
            Assert.False(queue.IsInQueue("2"));
        }

        /// <summary>
        /// Multiple players that join at different times and ranks.
        /// </summary>
        [Fact]
        public void MultiplePlayers()
        {
            queue.RoomSize = 2;
            queue.SearchExpansion = it => it * 1000;
            queue.SearchWidth = 1000;

            queue.AddToQueue("1", 5000);
            queue.AddToQueue("2", 0);

            string[][] sets = queue.Update().ToArray();
            Assert.Equal(0, sets.Length);

            // Another player joins, close to 1 in rank.
            queue.AddToQueue("3", 4000);
            sets = queue.Update().ToArray();
            Assert.Equal(1, sets.Length);
            Assert.Equal("1", sets[0][0]);
            Assert.Equal("3", sets[0][1]);
            Assert.True(queue.IsInQueue("2"));

            // Player 2 should not be matched up anymore.
            for (int i = 0; i < 10; i++)
            {
                sets = queue.Update().ToArray();
                Assert.Equal(0, sets.Length);
            }

            // Two more players join at the same rank.
            queue.AddToQueue("4", 1000);
            queue.AddToQueue("5", 1000);

            // Because player 2 has been waiting for a long time, they will have priority.
            sets = queue.Update().ToArray();
            Assert.Equal(1, sets.Length);
            Assert.Contains("2", sets[0]);

            // Clear the queue (assertions are difficult due to hashcode).
            queue.AddToQueue("6", 1000);
            sets = queue.Update().ToArray();
            Assert.Equal(1, sets.Length);
        }
    }
}
