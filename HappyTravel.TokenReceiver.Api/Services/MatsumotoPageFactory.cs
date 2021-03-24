using HappyTravel.TokenReceiver.Api.Options;
using Microsoft.Extensions.Options;

namespace HappyTravel.TokenReceiver.Api.Services
{
    public class MatsumotoPageFactory : PageFactory
    {
        public MatsumotoPageFactory(IOptions<BaseUrlOptions> options) : base(options)
        { }
    }
}