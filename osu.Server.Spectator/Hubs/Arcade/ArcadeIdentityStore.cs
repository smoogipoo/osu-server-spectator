// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using osu.Game.Arcade;

namespace osu.Server.Spectator.Hubs.Arcade
{
    public class ArcadeIdentityStore
    {
        private readonly ConcurrentDictionary<int, ArcadeIdentity> mappings = [];

        public void Add(int userId, ArcadeIdentity identity)
        {
            mappings[userId] = identity;
        }

        public bool TryGet(int userId, [NotNullWhen(true)] out ArcadeIdentity? identity)
        {
            return mappings.TryGetValue(userId, out identity);
        }

        public void Remove(int userId)
        {
            mappings.TryRemove(userId, out _);
        }
    }
}
