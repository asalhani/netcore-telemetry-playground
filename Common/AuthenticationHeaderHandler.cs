using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public class AuthenticationHeaderHandler: DelegatingHandler
    {
        public AuthenticationHeaderHandler()
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }
    }
}