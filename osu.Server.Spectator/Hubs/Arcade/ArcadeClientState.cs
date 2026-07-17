// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Server.Spectator.Hubs.Arcade
{
    [Serializable]
    public class ArcadeClientState : ClientState
    {
        public ArcadeClientState(in string connectionId, in int userId)
            : base(in connectionId, in userId)
        {
        }
    }
}
