// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Arcade;

namespace osu.Server.Spectator.Hubs.Arcade
{
    [Serializable]
    public class ArcadeClientState : ClientState
    {
        public readonly ArcadeIdentity Identity;

        public ArcadeClientState(in string connectionId, in int userId, ArcadeIdentity identity)
            : base(in connectionId, in userId)
        {
            Identity = identity;
        }
    }
}
