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
    pixelFormat = 3,
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

    public void FramebufferUpdateRequest(byte incremental = 0, ushort
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
            byte[] pixelData = new byte[p * rectWidth * rectHeight];
            byte[] rectData = new byte[12];
            ushort w = 0;
            ushort h = 0;
            byte[] mcastMessage = [ 0, 0, 0, 0, 0 ];
            int i;

            while (numberOfRectangles-- > 0)
            {
                socket.Receive(rectData, 0);
                w = BitConverter.ToUInt16([rectData[5], rectData[4]]);
                h = BitConverter.ToUInt16([rectData[7], rectData[6]]);
#if DEBUG
                Print("\nRect header: ", rectData);
                Print("Rect width x height: ", $"{w}x{h}");
#endif
                mcastMessage[0] = 0;
                mcastMessage[1] = width[0];
                mcastMessage[2] = width[1];
                mcastMessage[3] = height[0];
                mcastMessage[4] = height[1];

                multicastSocket.SendTo(mcastMessage, endPoint);

                mcastMessage[0]++;
                mcastMessage[1] = rectData[0];
                mcastMessage[2] = rectData[1];
                mcastMessage[3] = rectData[2];
                mcastMessage[4] = rectData[3];

                multicastSocket.SendTo(mcastMessage, endPoint);

                mcastMessage[0]++;
                mcastMessage[1] = rectData[4];
                mcastMessage[2] = rectData[5];
                mcastMessage[3] = rectData[6];
                mcastMessage[4] = rectData[7];
                
                multicastSocket.SendTo(mcastMessage, endPoint);

                mcastMessage[0]++;
                mcastMessage[1] = pixelFormat[0];

                multicastSocket.SendTo(mcastMessage, endPoint);

                mcastMessage[0]++;
                i = w * h;
                socket.Receive(pixelData, w * h * p, 0);

                while (i < w * h)
                {
                    mcastMessage[1] = pixelData[i * p];
                    mcastMessage[2] = pixelData[i * p + 1];
                    mcastMessage[3] = pixelData[i * p + 2];
                    mcastMessage[4] = pixelData[i * p + 3];
                    multicastSocket.SendTo(mcastMessage, endPoint);
                    i++;
                }
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