using Microsoft.Extensions;

namespace Softengi.UbClient.Configuration
{
    public class UnityBaseConnectionConfiguration
    {
        public string BaseUri { get; set; }
        public string AuthenticationMethod { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}