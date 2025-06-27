using System.Net;
using System.Net.Sockets;

namespace receiver;

public enum McastMessageType : byte
{
    ScreenBounds = 0,
    RectXY = 1,
    RectBounds = 2,
    PixelFormat = 3,
    PixelValue = 4
}

public class Receiver
{
    public Socket multicastSocket { get; }
    public IPEndPoint endPoint { get; }
    public byte[] data = new byte[5];

    public ushort rectWidth = 0;
    public ushort rectHeight = 0;
    public ushort rectX = 0;
    public ushort rectY = 0;
    public ushort Width = 0;
    public ushort Height = 0;
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

    public void ReceiveRectData()
    {
        byte[] data = new byte[5];

        do multicastSocket.Receive(data);
        while (data[0] != (byte)McastMessageType.ScreenBounds);

        if (Width == 0)
        {
            Width = BitConverter.ToUInt16([data[2], data[1]]);
            Height = BitConverter.ToUInt16([data[4], data[3]]);
        }

        do multicastSocket.Receive(data);
        while (data[0] != (byte)McastMessageType.RectXY);

        rectX = BitConverter.ToUInt16([data[2], data[1]]);
        rectY = BitConverter.ToUInt16([data[4], data[3]]);

        do multicastSocket.Receive(data);
        while (data[0] != (byte)McastMessageType.RectBounds);

        rectWidth = BitConverter.ToUInt16([data[2], data[1]]);
        rectHeight = BitConverter.ToUInt16([data[4], data[3]]);

        do multicastSocket.Receive(data);
        while (data[0] != (byte)McastMessageType.PixelFormat);

        if (pixelFormat == 0) pixelFormat = data[1];
    }

    public void ReceivePixel()
    {
        do multicastSocket.Receive(data);
        while (data[0] != (byte)McastMessageType.PixelValue);
    }

    public void Close()
    {
        multicastSocket.Shutdown(SocketShutdown.Receive);
        multicastSocket.Close();
        multicastSocket.Dispose();
    }
}