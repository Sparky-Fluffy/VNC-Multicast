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

    public ushort rectWidth { get; private set; }
    public ushort rectHeight { get; private set; }
    public ushort rectX { get; private set; }
    public ushort rectY { get; private set; }

    public byte[] pixelFormat { get; } = new byte[1];

    public Receiver(IPAddress multicastGroupAddress, ushort port)
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

        multicastSocket.SetSocketOption
        (
            SocketOptionLevel.IP, SocketOptionName.AddMembership, mcastOption
        );
    }

//     public void Connect()
//     {
//         multicastSocket.Receive(width);

// #if DEBUG
//         foreach (var t in width)
//             Console.WriteLine($"{t} ");
//         Console.WriteLine();
// #endif

//         multicastSocket.Receive(height);

// #if DEBUG
//         foreach (var t in height)
//             Console.WriteLine($"{t} ");
//         Console.WriteLine();
// #endif

//         multicastSocket.Receive(pixelFormat);

// #if DEBUG
//         foreach (var t in pixelFormat)
//             Console.WriteLine($"{t} ");
//         Console.WriteLine();
// #endif
//     }

    // public ushort ReceiveRectCount()
    // {
    //     byte[] rectCount = new byte[2];
    //     multicastSocket.Receive(rectCount);

    //     return BitConverter.ToUInt16(rectCount);
    // }

    public void ReceivePixelFormat()
    {
        multicastSocket.Receive(pixelFormat);

#if DEBUG
        Console.WriteLine(pixelFormat[0]);
#endif
    }

    public void ReceiveRectData()
    {
        byte[] rectData = new byte[13];
        multicastSocket.Receive(rectData);

        foreach (var item in rectData)
            Console.Write(item + " ");
        Console.WriteLine();

        rectX = BitConverter.ToUInt16([rectData[1], rectData[0]]);
        rectY = BitConverter.ToUInt16([rectData[3], rectData[2]]);
        rectWidth = BitConverter.ToUInt16([rectData[5], rectData[4]]);
        rectHeight = BitConverter.ToUInt16([rectData[7], rectData[6]]);
        pixelFormat[0] = rectData[12];
    }

    public byte[] ReceivePixels()
    {
        byte[] pixelData = new byte[pixelFormat[0] / 8 * rectWidth];
        multicastSocket.ReceiveAsync(pixelData);

        return pixelData;
    }
}