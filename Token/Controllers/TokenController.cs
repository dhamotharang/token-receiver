using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using Token.Models;
using Token.Services;

namespace Token.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TokenController : ControllerBase
    {
        public TokenController(IMemoryFlow flow, IOptions<BaseUrlOptions> options, PageFactory pageFactory)
        {
            _flow = flow;
            _options = options.Value;
            _pageFactory = pageFactory;
        }


        [HttpPost]
        public async Task<string> GetToken(LoginInfo login)
        {
            var cacheKey = _flow.BuildKey(nameof(TokenController), login.UserName);
            if (_flow.TryGetValue(cacheKey, out string token))
                return token;

            token = await GetTokenFromIdentity(login, _pageFactory, _options);

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

        
        private static async Task<string> GetTokenFromIdentity(LoginInfo login, PageFactory factory, BaseUrlOptions options)
        {
            var page = await factory.Get();

            await page.GoToAsync(options.Application);
            await page.WaitForNavigationAsync();

            await page.EvaluateExpressionAsync($"document.getElementById('Input_UserName').value = '{login.UserName}'; " +
                $"document.getElementById('Input_Password').value = '{login.Password}';");
            await page.ClickAsync("form button[type='submit']");

            try
            {
                //avoiding OPTIONS request from React
                for (var i = 0; i < MaxAttemptNumber; i++)
                {
                    var request = await page.WaitForRequestAsync($"{options.Api}agents");
                    if (request.Method == HttpMethod.Options)
                        continue;

                    await factory.Dispose(page);
                    return ParseToken(request);
                }
            }
            catch (TimeoutException ex)
            {
                throw new Exception($"Can't login the user with following credentials. Reason: {ex.Message}");
            }

            throw new Exception("Unable to obtain a token.");


            static string ParseToken(Request request1)
            {
                var headerValue = request1.Headers["authorization"];
                var header = AuthenticationHeaderValue.Parse(headerValue);

                return header.Parameter;
            }
        }


        private const int MaxAttemptNumber = 5;

        private readonly IMemoryFlow _flow;
        private readonly BaseUrlOptions _options;
        private readonly PageFactory _pageFactory;
    }
}