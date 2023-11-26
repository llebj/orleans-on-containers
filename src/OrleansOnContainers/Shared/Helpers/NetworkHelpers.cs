using System.Net;

namespace Shared.Helpers;

public static class NetworkHelpers
{
    public static IPEndPoint GetIpEndpoint(string hostname, int port)
    {
        var host = Dns.GetHostEntry(hostname);

        return new IPEndPoint(host.AddressList[0], port);
    }
}
