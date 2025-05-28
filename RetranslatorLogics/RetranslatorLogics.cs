using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RetranslatorLogics;

enum CloseProxyStatus : byte
{
    Success = 0,
    Failed = 1
}

public enum ReceivingRectangles : byte
{
    Incremental = 0,
    NoIncremental = 1
}

public enum ServerMessageTypes : byte
{
    FramebufferUpdate = 0,
    SetColourMapEntries = 1,
    Bell = 2,
    ServerCutText = 3
}

public enum ClientMessageTypes : byte
{
    SetPixelFormat = 0,
    SetEncodings = 2,
    FramebufferUpdateRequest = 3,
    KeyEvent = 4,
    PointerEvent = 5,
    ClientCutText = 6,
}

public enum Encodings
{
    Raw = 0,
    CopyRect = 1,
    RRE = 2,
    Hextile = 5,
    TRLE = 15,
    ZRLE = 16,
    CursorPseudoEncoding = -239,
    DesktopSizePseudoEncoding = -223
}

public enum McastMessageType : byte
{
    ScreenBounds = 0,
    RectXY = 1,
    RectBounds = 2,
    PixelFormat = 3,
    PixelValue = 4
}

public class Retranslator
{
    public int port { get; }
    public IPAddress ip { get; }
    public Socket socket { get; }
    public Encodings encodingType { get; }
    public byte[] width { get; } = new byte[2];
    public byte[] height { get; } = new byte[2];
    public byte[] pixelFormat { get; } = new byte[16];
    public Socket multicastSocket { get; }
    public IPEndPoint endPoint { get; }

    public Retranslator(IPAddress ip, int port, Encodings encodingType,
            IPAddress multicastGroupAddress, int multicastPort, IPAddress
            localIP, int interfaceIndex)
    {
        try
        {
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                    ProtocolType.Tcp);
            this.ip = ip;
            this.port = port;
            this.encodingType = encodingType;

            multicastSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);
            endPoint = new IPEndPoint(multicastGroupAddress, multicastPort);
            multicastSocket.Bind(new IPEndPoint(localIP, 0));

            MulticastOption mcastOption = new
                MulticastOption(multicastGroupAddress, localIP);
            mcastOption.InterfaceIndex = interfaceIndex;

            multicastSocket.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.AddMembership, mcastOption);
        }
        catch (Exception e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.Failed);
        }
    }

    private void SetProtocolVersion()
    {
        try
        {
            socket.Connect(ip, port);

            byte[] protocolVersion = new byte[12];
            socket.Receive(protocolVersion, protocolVersion.Length, 0);
            socket.Send(protocolVersion, protocolVersion.Length, 0);
        }
        catch (Exception e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.Failed);
        }
    }

    private byte[] GetSecurityTypes()
    {
        byte[] numberOfSecurity = new byte[1];
        socket.Receive(numberOfSecurity, numberOfSecurity.Length, 0);

        byte[] securityTypes = new byte[numberOfSecurity[0]];
        socket.Receive(securityTypes, securityTypes.Length, 0);

        return securityTypes;
    }

    private void MakeHandshakes()
    {
        try
        {
            SetProtocolVersion();
            GetSecurityTypes();

            byte[] securityType = [1];
            socket.Send(securityType, securityType.Length, 0);

            byte[] securityHandshake = new byte[4];
            socket.Receive(securityHandshake, securityHandshake.Length, 0);
        }
        catch (Exception ex)
        {
            ExitProcessRetranslator(ex.Message, CloseProxyStatus.Failed);
        }
    }

    private void ClientInit()
    {
        byte[] sharedFlag = [0];
        try
        {
            socket.Send(sharedFlag, sharedFlag.Length, 0);
        }
        catch (Exception ex)
        {
            ExitProcessRetranslator(ex.Message, CloseProxyStatus.Failed);
        }
    }

    private void ServerInit()
    {
        try
        {
            socket.Receive(width, width.Length, 0);
            socket.Receive(height, height.Length, 0);
            socket.Receive(pixelFormat, pixelFormat.Length, 0);

            byte[] nameLenght = new byte[4];

            socket.Receive(nameLenght, nameLenght.Length, 0);

            Array.Reverse(nameLenght);
            int nameLenghtNumber = BitConverter.ToInt32(nameLenght, 0);

            byte[] nameString = new byte[nameLenghtNumber];
            socket.Receive(nameString, nameString.Length, 0);
        }
        catch (Exception e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.Failed);
        }
    }

    private void SetEncoding()
    {
        try
        {
            byte[] numberOfEncodings = [
                (byte)ClientMessageTypes.SetEncodings, 0, 0, 1 ];
            socket.Send(numberOfEncodings, numberOfEncodings.Length, 0);

            byte[] encodingTypeMsg = [0, 0, 0, (byte)encodingType];
            socket.Send(encodingTypeMsg, encodingTypeMsg.Length, 0);
        }
        catch (Exception e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.Failed);
        }
    }

    public void SetPixelFormat()
    {
        try
        {
            byte[] msg = [
                (byte)ClientMessageTypes.SetPixelFormat, 0, 0, 0,
                pixelFormat[0], pixelFormat[1], pixelFormat[2], pixelFormat[3],
                pixelFormat[4], pixelFormat[5], pixelFormat[6], pixelFormat[7],
                pixelFormat[8], pixelFormat[9], pixelFormat[10],
                pixelFormat[11], pixelFormat[12], pixelFormat[13],
                pixelFormat[14], pixelFormat[15] ];
            socket.Send(msg, msg.Length, 0);

        }
        catch (Exception e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.Failed);
        }
    }

    public unsafe void FramebufferUpdateRequest(byte incremental = 0, ushort
            XPosition = 0, ushort YPosition = 0,
            ushort rectWidth = 200, ushort rectHeight = 200)
    {
        try
        {
            byte[] updateRequest = [
                (byte)ClientMessageTypes.FramebufferUpdateRequest, incremental,
                (byte)(XPosition >> 8), (byte)XPosition, (byte)(YPosition >> 8),
                (byte)YPosition, (byte)(rectWidth >> 8), (byte)rectWidth,
                (byte)(rectHeight >> 8), (byte)rectHeight
            ];

            socket.Send(updateRequest, updateRequest.Length, 0);

            byte[] countRects = new byte[4];
            socket.Receive(countRects, countRects.Length, 0);
            ushort numberOfRectangles = BitConverter.ToUInt16([countRects[3],
                    countRects[2]], 0);

            byte p = (byte)(pixelFormat[0] / 8);
            ushort w = 0;
            ushort h = 0;

            byte[] screenBoundsMsg = [(byte)McastMessageType.ScreenBounds, .. width, .. height];
            byte[] rectXYMsg = [(byte)McastMessageType.RectXY, 0, 0, 0, 0];
            byte[] rectBoundsMsg = [(byte)McastMessageType.RectBounds, 0, 0, 0, 0];
            byte[] pixelFormatMsg = [(byte)McastMessageType.PixelFormat, pixelFormat[0], 0, 0, 0];
            byte[] pixelValueMsg = [(byte)McastMessageType.PixelValue, 0, 0, 0, 0];
            byte[] encodingMsg = new byte[4];
            byte[] pixelData = new byte[p * 1920 * 1080];

            int buf = 0;
            int i = 0;
            while (numberOfRectangles-- > 0)
            {
                Print("aaaaaaa");
                socket.Receive(rectXYMsg, 1, 4, 0);
                socket.Receive(rectBoundsMsg, 1, 4, 0);
                socket.Receive(encodingMsg);

                w = (ushort)(rectBoundsMsg[2] + (ushort)(rectBoundsMsg[1] << 8));
                h = (ushort)(rectBoundsMsg[4] + (ushort)(rectBoundsMsg[3] << 8));
                buf = w * h * p;
#if DEBUG
                Print("Rect width x height: ", $"{w}x{h}");
                Print("Rect XY: ", rectXYMsg);
                Print("Rect bounds: ", rectBoundsMsg);
#endif

                socket.Receive(pixelData, buf, 0);
                Task.Run
                (
                    () =>
                    {
                        multicastSocket.SendTo(screenBoundsMsg, endPoint);
                        multicastSocket.SendTo(rectXYMsg, endPoint);
                        multicastSocket.SendTo(rectBoundsMsg, endPoint);
                        multicastSocket.SendTo(pixelFormatMsg, endPoint);
                        i = 0;
                        while (i < buf)
                        {
                            pixelValueMsg[1] = pixelData[i];
                            pixelValueMsg[2] = pixelData[i + 1];
                            pixelValueMsg[3] = pixelData[i + 2];
                            pixelValueMsg[4] = pixelData[i + 3];
                            multicastSocket.SendTo(pixelValueMsg, endPoint);
                            i += p;
                        }
                    }
                );
                Print("bbbbbbb");
            }
        }
        catch (Exception e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.Failed);
        }
    }

    public void Connect()
    {
        MakeHandshakes();
        ClientInit();
        ServerInit();
        SetEncoding();
    }

    #region ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
    private void ExitProcessRetranslator(string msg, CloseProxyStatus st)
    {
        Console.WriteLine(msg);
        Environment.Exit((int)st);
    }

#if DEBUG
    public void Print<T>(string msg, T? val) =>
        Print(msg, () => Console.Write(val));

    public void Print<T>(string msg, T[] val) =>
        Print(msg, () => { foreach (var s in val) Console.Write($"{s} "); });

    public void Print(string msg, Action? action = null)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(msg);

        Console.ForegroundColor = ConsoleColor.Yellow;
        action?.Invoke();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
    }
#endif

    public void CloseAndFree()
    {
        try
        {
            socket.Disconnect(true);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket.Dispose();
            multicastSocket.Close();
            multicastSocket.Dispose();
        }
        catch (Exception e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.Failed);
        }
    }
}
#endregion