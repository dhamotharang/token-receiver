using System.Collections.Generic;
using HappyTravel.TokenReceiver.Api.Models;

namespace HappyTravel.TokenReceiver.Api.Options
{
    public class ApplicationOptions
    {
        public Dictionary<Applications, BaseUrlOptions> Store { get; } = new Dictionary<Applications, BaseUrlOptions>();
    }
}