using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using HappyTravel.TokenReceiver.Api.Models;
using HappyTravel.TokenReceiver.Api.Options;
using HappyTravel.TokenReceiver.Api.Services;

namespace HappyTravel.TokenReceiver.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TokenController : ControllerBase
    {
        public TokenController(IMemoryFlow flow, IOptions<ApplicationOptions> options, PageFactoryManager pageFactoryManager)
        {
            _flow = flow;
            _options = options.Value;
            _pageFactoryManager = pageFactoryManager;
        }


        [HttpPost("{app}")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetToken(LoginInfo login, [FromRoute] Applications app)
        {
            var cacheKey = _flow.BuildKey(nameof(TokenController), app.ToString(), login.UserName);
            if (_flow.TryGetValue(cacheKey, out string token))
                return Ok(token);

            token = await GetTokenFromIdentity(login, _pageFactoryManager.GetFactory(app), _options.Store[app]);

            _flow.Set(cacheKey, token, new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = GetCachingThreshold(token)
            });
            return Ok(token);
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

            try
            {
                await page.GoToAsync(options.Application);
                await page.WaitForNavigationAsync();

                await page.EvaluateExpressionAsync($"document.getElementById('Input_UserName').value = '{login.UserName}';" +
                    $"document.getElementById('Input_Password').value = '{login.Password}';" + 
                     "document.getElementsByClassName('button')[0].click();");

                //avoiding OPTIONS request from React
                for (var i = 0; i < MaxAttemptNumber; i++)
                {
                    var request = await page.WaitForRequestAsync($"{options.ApiEndpoint}");
                    if (request.Method == HttpMethod.Options)
                        continue;

                    return ParseToken(request);
                }
            }
            catch (TimeoutException ex)
            {
                throw new Exception($"Can't login the user with following credentials. Reason: {ex.Message}");
            }
            catch (Exception)
            {
                throw new Exception("Unable to obtain a token.");
            }
            finally
            {
                await factory.Dispose(page);
            }

            return string.Empty;
            

            static string ParseToken(Request request)
            {
                var headerValue =
                    request.Headers.Single(h => h.Key.ToUpperInvariant().Equals("AUTHORIZATION")).Value;
                var header = AuthenticationHeaderValue.Parse(headerValue);

                return header.Parameter;
            }
        }


        private const int MaxAttemptNumber = 5;

        private readonly IMemoryFlow _flow;
        private readonly ApplicationOptions _options;
        private readonly PageFactoryManager _pageFactoryManager;
    }
}