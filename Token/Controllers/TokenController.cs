using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PuppeteerSharp;
using Token.Models;

namespace Token.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TokenController : ControllerBase
    {
        public TokenController(IMemoryFlow flow)
        {
            _flow = flow;
        }


        [HttpPost]
        public async Task<string> GetToken(LoginInfo login)
        {
            var cacheKey = _flow.BuildKey(nameof(TokenController), login.UserName);
            if (_flow.TryGetValue(cacheKey, out string result))
                return result;

            var token = await GetTokenFromIdentity(login);

            _flow.Set(cacheKey, token, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = GetCachingThreshold(token)
            });
            return token;
        }


        private static DateTimeOffset GetCachingThreshold(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var parsedToken = handler.ReadJwtToken(token);
            var timestamp = parsedToken.Payload.Exp;
            if (!timestamp.HasValue)
                return DateTimeOffset.Now;

            var dtDateTime = new DateTime(1970,1,1,0,0,0,0,DateTimeKind.Utc)
                .AddSeconds(timestamp.Value)
                .AddSeconds(-30)
                .ToLocalTime();

            return new DateTimeOffset(dtDateTime);
        }


        private static async ValueTask<Page> GetNewPage()
        {
            if (_browser is null)
            {
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new []{"--no-sandbox --disable-setuid-sandbox --incognito"}
                });
            }

            return await _browser.NewPageAsync();
        }

        
        private static async Task<string> GetTokenFromIdentity(LoginInfo login)
        {
            using var page = await GetNewPage();

            await page.GoToAsync("https://dev.happytravel.com/", new NavigationOptions {WaitUntil = new[] {WaitUntilNavigation.DOMContentLoaded}});
            await page.WaitForNavigationAsync();

            await page.EvaluateExpressionAsync($"document.getElementById('Input_UserName').value = '{login.UserName}'; " +
                $"document.getElementById('Input_Password').value = '{login.Password}';");
            await page.ClickAsync("form button[type='submit']");

            //avoiding OPTIONS request from React
            for (var i = 0; i < MaxAttemptNumber; i++)
            {
                var request = await page.WaitForRequestAsync("https://edo-api.dev.happytravel.com/en/api/1.0/locations/regions");
                if (request.Method == HttpMethod.Options)
                    continue;

                await page.GoToAsync("https://dev.happytravel.com/logout", new NavigationOptions {WaitUntil = new[] {WaitUntilNavigation.DOMContentLoaded}});
                await page.WaitForNavigationAsync();

                return ParseToken(request);
            }

            throw new Exception("Unable to obtain a token.");


            static string ParseToken(Request request1)
            {
                var headerValue = request1.Headers["authorization"];
                var header = AuthenticationHeaderValue.Parse(headerValue);

                return header.Parameter;
            }
        }


        private static Browser _browser;
        private const int MaxAttemptNumber = 5;

        private readonly IMemoryFlow _flow;
    }
}