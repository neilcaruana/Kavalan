using System.Net;

namespace Kavalan.Core;
public static class NetworkHelper
{
    public static IPAddress GetEndpointIPAddress(EndPoint? endPoint)
    {
        if (endPoint is IPEndPoint remoteEndPoint)
            return remoteEndPoint.Address;
        else
            throw new ArgumentException($"Invalid type {endPoint?.GetType()}", nameof(endPoint));
    }
    public static int GetEndpointPort(EndPoint? endPoint)
    {
        if (endPoint is IPEndPoint remoteEndPoint)
            return remoteEndPoint.Port;
        else
            throw new ArgumentException($"Invalid type {endPoint?.GetType()}", nameof(endPoint));
    }
    public static bool IsPublicIpAddress(IPAddress? ip)
    {
        if (ip == null)
            return false;

        byte[] addressBytes = ip.GetAddressBytes();
        byte first = addressBytes[0];
        byte second = addressBytes[1];

        if (first == 10)
            return false; // 10.0.0.0/8
        if (first == 172 && second >= 16 && second <= 31)
            return false; // 172.16.0.0/12
        if (first == 192 && second == 168)
            return false; // 192.168.0.0/16

        return true;
    }
}
