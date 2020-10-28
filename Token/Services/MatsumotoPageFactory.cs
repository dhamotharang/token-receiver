using Microsoft.Extensions.Options;

namespace Token.Services
{
    public class MatsumotoPageFactory : PageFactory
    {
        public MatsumotoPageFactory(IOptions<BaseUrlOptions> options) : base(options)
        { }
    }
}