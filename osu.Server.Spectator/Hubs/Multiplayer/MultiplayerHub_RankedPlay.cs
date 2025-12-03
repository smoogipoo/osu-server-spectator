// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using osu.Game.Online.Multiplayer.MatchTypes.RankedPlay;
using osu.Game.Online.RankedPlay;

namespace osu.Server.Spectator.Hubs.Multiplayer
{
    public partial class MultiplayerHub : IRankedPlayServer
    {
        public Task<RankedPlayDiscardResponse> DiscardCards(RankedPlayCard[] cards)
        {
            throw new System.NotImplementedException();
        }

        public Task PlayCard(RankedPlayCard card)
        {
            throw new System.NotImplementedException();
        }
    }
}
