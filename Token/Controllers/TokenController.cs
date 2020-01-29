using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using Token.Models;

namespace Token.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TokenController : ControllerBase
    {
        public TokenController(IMemoryFlow flow, IHttpClientFactory clientFactory)
        {
            _flow = flow;
            _clientFactory = clientFactory;
        }


        [HttpPost]
        public async Task<string> GetToken(LoginInfo login)
        {
            var cacheKey = _flow.BuildKey(nameof(TokenController), login.UserName);
            if (_flow.TryGetValue(cacheKey, out string result) && await IsValidToken(result))
                return result;

            var token = await GetTokenFromIdentity();

            _flow.Set(cacheKey, token, DefaultLocationCachingTime);
            return token;


            async Task<bool> IsValidToken(string authToken)
            {
                Client.SetBearerToken(authToken);
                using var response = await Client.GetAsync("/en/api/1.0/customers");
                return response.IsSuccessStatusCode;
            }


            async Task<string> GetTokenFromIdentity()
            {
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    Args = new []{"--no-sandbox --disable-setuid-sandbox"}
                });

                var page = await browser.NewPageAsync();

                await page.GoToAsync("https://dev.happytravel.com/", new NavigationOptions {WaitUntil = new[] {WaitUntilNavigation.DOMContentLoaded}});
                await page.WaitForNavigationAsync();
                await page.EvaluateExpressionAsync($"document.getElementById('Input_UserName').value = '{login.UserName}';");
                await page.EvaluateExpressionAsync($"document.getElementById('Input_Password').value = '{login.Password}';");
                await page.ClickAsync("form button[type='submit']");

                var request1 = await page.WaitForRequestAsync("https://edo-api.dev.happytravel.com/en/api/1.0/locations/regions");
                var request2 = await page.WaitForRequestAsync("https://edo-api.dev.happytravel.com/en/api/1.0/locations/regions");
                var token = ParseToken(request1.Method == HttpMethod.Get ? request1 : request2);
                return token;


                string ParseToken(Request request)
                {
                    var headerValue = request.Headers["authorization"];
                    var header = AuthenticationHeaderValue.Parse(headerValue);
                    return header.Parameter;
                }
            }
        }


        private HttpClient Client => _client ??= _clientFactory.CreateClient("edo");


        private static readonly TimeSpan DefaultLocationCachingTime = TimeSpan.FromMinutes(9.5);
        private readonly IHttpClientFactory _clientFactory;
        private readonly IMemoryFlow _flow;
        private HttpClient _client;
    }
}