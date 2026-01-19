// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using osu.Server.Spectator.Hubs.Metadata;

namespace osu.Server.Spectator
{
    public class MethodRateLimiter : IHubFilter
    {
        /// <summary>
        /// Rate limiter for <see cref="MetadataHub.RefreshFriends"/>.
        /// </summary>
        private static readonly PartitionedRateLimiter<string> refresh_friends_limiter = PartitionedRateLimiter.Create<string, string>(resource =>
        {
            return RateLimitPartition.GetFixedWindowLimiter(resource, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromSeconds(1),
            });
        });

        private static PartitionedRateLimiter<string> getRateLimiter(string policy)
        {
            switch (policy)
            {
                case nameof(MetadataHub.RefreshFriends):
                    return refresh_friends_limiter;

                default:
                    throw new ArgumentException($"No rate limiter defined for policy: {policy}");
            }
        }

        public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
        {
            EnableRateLimitingAttribute? attribute = invocationContext.HubMethod.GetCustomAttribute<EnableRateLimitingAttribute>();

            if (attribute?.PolicyName == null)
                return await next(invocationContext);

            using (var lease = await getRateLimiter(attribute.PolicyName).AcquireAsync(invocationContext.Context.ConnectionId, cancellationToken: invocationContext.Context.ConnectionAborted))
            {
                if (lease.IsAcquired)
                    return await next(invocationContext);
            }

            return null;
        }
    }
}
