// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace osu.Server.Spectator.Services
{
    public class Discord : IDiscord
    {
        private readonly HttpClient httpClient;

        public Discord()
        {
            httpClient = new HttpClient();
        }

        public async Task SendMessageAsync(DiscordServer target, string content)
        {
            string json = JsonSerializer.Serialize(new { content = content });
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            string webhookUrl = target switch
            {
                DiscordServer.ArcadeStore => AppSettings.ArcadeStoreUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };

            var response = await httpClient.PostAsync(webhookUrl, httpContent);
            response.EnsureSuccessStatusCode();
        }
    }
}
