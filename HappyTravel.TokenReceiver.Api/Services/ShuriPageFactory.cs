using HappyTravel.TokenReceiver.Api.Options;
using Microsoft.Extensions.Options;
namespace HappyTravel.TokenReceiver.Api.Services
{
    public class ShuriPageFactory : PageFactory
    {
        public ShuriPageFactory(IOptions<BaseUrlOptions> options) : base(options)
        { }
    }
}