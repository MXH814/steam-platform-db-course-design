using System.Net;
using System.Net.Sockets;

namespace SteamPlatform.HttpsDeploy;

public static class LoopbackHttpClientFactory
{
    public static HttpClient Create(bool allowAutoRedirect, TimeSpan timeout)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = allowAutoRedirect,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var client = new TcpClient(AddressFamily.InterNetwork);
                try
                {
                    await client.ConnectAsync(IPAddress.Loopback, context.DnsEndPoint.Port, cancellationToken);
                    return client.GetStream();
                }
                catch
                {
                    client.Dispose();
                    throw;
                }
            }
        };

        return new HttpClient(handler, disposeHandler: true) { Timeout = timeout };
    }
}
