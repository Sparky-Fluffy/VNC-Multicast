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

    public Socket multicastSocket { get; }
    public IPEndPoint endPoint { get; }

    private byte[] updateRequest = [ (byte)ClientMessageTypes.FramebufferUpdateRequest,
                                        0, 0, 0,0, 0, 0, 0, 0, 0 ];
    byte[] countRects = new byte[4];
    ushort numberOfRectangles = 0;

    private byte[] width { get; } = new byte[2];
    private byte[] height { get; } = new byte[2];
    private byte[] pixelFormat { get; } = new byte[16];

    public ushort ScreenWidth { get; private set; } = 0;
    public ushort ScreenHeight { get; private set; } = 0;
    private ushort rectWidth = 0;
    private ushort rectHeight = 0;
    public byte Bpp { get; private set; } = 0;
    private byte[] screenBoundsMsg = [(byte)McastMessageType.ScreenBounds, 0, 0, 0, 0];
    private byte[] rectXYMsg = [(byte)McastMessageType.RectXY, 0, 0, 0, 0];
    private byte[] rectBoundsMsg = [(byte)McastMessageType.RectBounds, 0, 0, 0, 0];
    private byte[] pixelFormatMsg = [(byte)McastMessageType.PixelFormat, 0, 0, 0, 0];
    private byte[] pixelValueMsg = [(byte)McastMessageType.PixelValue, 0, 0, 0, 0];
    private byte[] encodingMsg = new byte[4];
    private byte[] pixelData;

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
            ScreenWidth = (ushort)(width[1] + (ushort)(width[0] << 8));

            socket.Receive(height, height.Length, 0);
            ScreenHeight = (ushort)(height[1] + (ushort)(height[0] << 8));

            socket.Receive(pixelFormat, pixelFormat.Length, 0);
            Bpp = (byte)(pixelFormat[0] / 8);

            screenBoundsMsg[1] = width[0];
            screenBoundsMsg[2] = width[1];
            screenBoundsMsg[3] = height[0];
            screenBoundsMsg[4] = height[1];

            pixelFormatMsg[1] = Bpp;
            pixelData = new byte[Bpp * ScreenWidth * ScreenHeight];

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
            ushort frameWidth = 200, ushort frameHeight = 200)
    {
        try
        {
            updateRequest[1] = incremental;
            updateRequest[2] = (byte)(XPosition >> 8);
            updateRequest[3] = (byte)XPosition;
            updateRequest[4] = (byte)(YPosition >> 8);
            updateRequest[5] = (byte)YPosition;
            updateRequest[6] = (byte)(frameWidth >> 8);
            updateRequest[7] = (byte)frameWidth;
            updateRequest[8] = (byte)(frameHeight >> 8);
            updateRequest[9] = (byte)frameHeight;

            socket.Send(updateRequest, updateRequest.Length, 0);
            socket.Receive(countRects, countRects.Length, 0);
            numberOfRectangles = (ushort)(countRects[3] + (ushort)(countRects[2] << 8));

            int buf = 0;
            while (numberOfRectangles-- > 0)
            {
                //Print("aaaaaaa");
                socket.Receive(rectXYMsg, 1, 4, 0);
                socket.Receive(rectBoundsMsg, 1, 4, 0);
                socket.Receive(encodingMsg);

                rectWidth = (ushort)(rectBoundsMsg[2] + (ushort)(rectBoundsMsg[1] << 8));
                rectHeight = (ushort)(rectBoundsMsg[4] + (ushort)(rectBoundsMsg[3] << 8));
                buf = rectWidth * rectHeight;
#if DEBUG
                Print("Rect width x height: ", $"{rectWidth}x{rectHeight}");
                //Print("Rect XY: ", rectXYMsg);
                //Print("Rect bounds: ", rectBoundsMsg);
#endif
                //socket.Receive(pixelData, buf, 0);
                multicastSocket.SendTo(screenBoundsMsg, endPoint);
                multicastSocket.SendTo(rectXYMsg, endPoint);
                multicastSocket.SendTo(rectBoundsMsg, endPoint);
                multicastSocket.SendTo(pixelFormatMsg, endPoint);
                while (buf-- > 0)
                {
                    socket.Receive(pixelValueMsg, 1, 4, 0);
                    multicastSocket.SendTo(pixelValueMsg, endPoint);
                }
                // Task.Run
                // (
                //     () =>
                //     {

                //     }
                // );
                //Print("bbbbbbb");
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