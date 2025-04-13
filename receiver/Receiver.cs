using System.Net;
using System.Net.Sockets;

namespace receiver;

public class Receiver
{
    public Socket multicastSocket { get; }
    public IPEndPoint endPoint { get; }
    public byte[] width { get; } = new byte[2];
    public byte[] height { get; } = new byte[2];
    public byte[] pixelFormat { get; } = new byte[1];

    Receiver(IPAddress multicastGroupAddress)
    {
        multicastSocket = new Socket
        (
            AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp
        );

        endPoint = new IPEndPoint(IPAddress.Any, 8001);

        multicastSocket.Bind(endPoint);
        
        MulticastOption mcastOption = new MulticastOption
        (
            multicastGroupAddress, IPAddress.Any
        );

        multicastSocket.SetSocketOption
        (
            SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption
        );
    }

    void Connect()
    {
        multicastSocket.Receive(width);

#if DEBUG
        foreach (var t in width)
            Console.WriteLine($"{t} ");
        Console.WriteLine();
#endif

        multicastSocket.Receive(height);

#if DEBUG
        foreach (var t in height)
            Console.WriteLine($"{t} ");
        Console.WriteLine();
#endif

        multicastSocket.Receive(pixelFormat);

#if DEBUG
        foreach (var t in height)
            Console.WriteLine($"{t} ");
        Console.WriteLine();
#endif
    }

    ushort ReceiveRectCount()
    {
        byte[] rectCount = new byte[2];
        multicastSocket.Receive(rectCount);

        return BitConverter.ToUInt16(rectCount);
    }

    ushort[] ReceiveRectData()
    {
        byte[] rectData = new byte[12];
        multicastSocket.Receive(rectData);

        return
        [
            BitConverter.ToUInt16([rectData[1], rectData[0]]),
            BitConverter.ToUInt16([rectData[3], rectData[2]]),
            BitConverter.ToUInt16([rectData[5], rectData[4]]),
            BitConverter.ToUInt16([rectData[7], rectData[6]])
        ];
    }

    byte[] ReceivePixel()
    {
        byte[] pixelData = new byte[pixelFormat[0] / 8];
        multicastSocket.Receive(pixelData);

        return pixelData;
    }
}