using System.Net;
using System.Net.Sockets;

namespace receiver;

public class Receiver
{
    public Socket multicastSocket { get; }
    public IPEndPoint endPoint { get; }

    public ushort Width { get => BitConverter.ToUInt16([width[1], width[0]]); }
    public ushort Height { get => BitConverter.ToUInt16([height[1], height[0]]); }

    public byte[] width { get; } = new byte[2];
    public byte[] height { get; } = new byte[2];

    public byte[] pixelData;

    public ushort rectWidth { get; private set; } = 0;
    public ushort rectHeight { get; private set; } = 0;
    public ushort rectX = 0;
    public ushort rectY = 0;

    public byte pixelFormat = 0;

    public Receiver(IPAddress multicastGroupAddress, ushort port, int ifaceIndex)
    {
        multicastSocket = new Socket
        (
            AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp
        );

        endPoint = new IPEndPoint(IPAddress.Any, port);

        multicastSocket.Bind(endPoint);

        MulticastOption mcastOption = new MulticastOption
        (
            multicastGroupAddress, IPAddress.Any
        );
        mcastOption.InterfaceIndex = ifaceIndex;
        multicastSocket.SetSocketOption
        (
            SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption
        );
    }

    public void ReceivePixelFormat()
    {
        multicastSocket.Receive([pixelFormat]);

#if DEBUG
        Console.WriteLine(pixelFormat);
#endif
    }

    public void ReceiveRectData()
    {
        byte[] rectData = new byte[13];
        multicastSocket.Receive(rectData);

        foreach (var item in rectData)
            Console.Write(item + " ");
        Console.WriteLine();

        if (rectWidth == 0)
        {
            rectWidth = BitConverter.ToUInt16([rectData[5], rectData[4]]);
            rectHeight = BitConverter.ToUInt16([rectData[7], rectData[6]]);
            pixelFormat = (byte)(rectData[12] / 8);
            pixelData = new byte[pixelFormat * rectWidth];
        }

        rectX = BitConverter.ToUInt16([rectData[1], rectData[0]]);
        rectY = BitConverter.ToUInt16([rectData[3], rectData[2]]);
    }

    public void ReceivePixels() => multicastSocket.ReceiveAsync(pixelData);

    public void Close()
    {
        multicastSocket.Shutdown(SocketShutdown.Receive);
        multicastSocket.Close();
        multicastSocket.Dispose();
    }
}
