using System;
using System.Net;
using System.Net.Sockets;

namespace proxy;

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

public class Retranslator
{
    public int _port { get; }
    public IPAddress _ip { get; }
    public Socket _socket { get; }
    public Encodings _encodingType { get; }
    public byte[] _width { get; } = new byte[2];
    public byte[] _height { get; } = new byte[2];

    public Retranslator(IPAddress ip, int port, Encodings encodingType)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
        _ip = ip;
        _port = port;
        _encodingType = encodingType;
    }

    private void ServerInit()
    {
        byte[] serverInfo = new byte[64];
        _socket.Receive(serverInfo, serverInfo.Length, 0);
        _width[0] = serverInfo[3];
        _width[1] = serverInfo[4];
        _height[0] = serverInfo[5];
        _height[1] = serverInfo[6];

#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nServer Init message data");
        Console.ForegroundColor = ConsoleColor.Yellow;
        foreach (byte s in serverInfo)
            Console.Write($"{s} ");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
#endif
    }

    public void SetPixelFormat(byte pitsPerPixel, byte depth, byte
            bigEndianFlag, byte trueColorFlag, uint redMax, uint greenMax, uint
            blueMax, byte redShift, byte greenShift, byte blueShift)
    {
        byte[] msg = new byte[] { (byte)ClientMessageTypes.SetPixelFormat, 0, 0,
            0, pitsPerPixel, depth, bigEndianFlag, trueColorFlag,
            (byte)(redMax >> 8), (byte)redMax, (byte)(greenMax >> 8),
            (byte)greenMax, (byte)(blueMax >> 8), (byte)blueMax, redShift,
            greenShift, blueShift, 0, 0, 0 };
        _socket.Send(msg, msg.Length, 0);
    }

    public void FramebufferUpdateRequest(byte incremental = 0, ushort
            XPosition = 0, ushort YPosition = 0)
    {
        byte[] updateRequest = new byte[]
        { (byte)ClientMessageTypes.FramebufferUpdateRequest, incremental,
            (byte)(XPosition >> 8), (byte)XPosition, (byte)(YPosition >> 8),
            (byte)YPosition, _width[0], _width[1], _height[0], _height[1] };

#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nFrame buffer update request");
        Console.ForegroundColor = ConsoleColor.Yellow;
        foreach (byte b in updateRequest)
            Console.Write($"{b} ");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
#endif

        _socket.Send(updateRequest, updateRequest.Length, 0);

        byte[] frameBufferUpdateMessageResponse = new byte[4];
        _socket.ReceiveAsync(frameBufferUpdateMessageResponse, 0);
#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nFrame Buffer Update Message Response");
        Console.ForegroundColor = ConsoleColor.Yellow;
        foreach (byte s in frameBufferUpdateMessageResponse)
            Console.Write($"{s} ");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
#endif

#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        /* Console.WriteLine("\nFrame Buffer Update Message Response");
        Console.ForegroundColor = ConsoleColor.Yellow;
        foreach (byte s in frameBufferUpdateMessageResponse)
            Console.Write($"{s} ");
        Console.WriteLine(); */
        Console.ForegroundColor = ConsoleColor.White;
#endif
    }

    private void SetEncoding()
    {
#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\nSet encoding message: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(_encodingType);
        Console.ForegroundColor = ConsoleColor.White;
#endif
        byte[] msg = new byte[] { (byte)ClientMessageTypes.SetEncodings, 0,
            (byte)_encodingType };
        _socket.Send(msg, msg.Length, 0);
    }

    private void setProtocolVersion()
    {
        _socket.Connect(_ip, _port);

        byte[] protocolVersion = new byte[12];
        _socket.Receive(protocolVersion, protocolVersion.Length, 0);
#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nProtocolVersion");
        Console.ForegroundColor = ConsoleColor.Yellow;
        foreach (byte p in protocolVersion)
            Console.Write($"{p} ");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
#endif
        _socket.Send(protocolVersion, protocolVersion.Length, 0);
    }

    private byte[] getSecurityTypes()
    {
        byte[] numberOfSecurity = new byte[1];
        _socket.Receive(numberOfSecurity, numberOfSecurity.Length, 0);

#if DEBUG
        Console.WriteLine($"\nNumber of security types: {numberOfSecurity[0]}");
#endif

        byte[] securityTypes = new byte[numberOfSecurity[0]];
        _socket.Receive(securityTypes, securityTypes.Length, 0);

#if DEBUG
        Console.Write("Security types: ");
        foreach (var t in securityTypes)
            Console.Write($"{t} ");
        Console.WriteLine();
#endif
        return securityTypes;
    }

    private void makeHandshakes()
    {
        try
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nПопытка подключения к серверу...");
            Console.ForegroundColor = ConsoleColor.White;
#endif
            setProtocolVersion();
            getSecurityTypes();

            byte[] securityType = new byte[] { 1 };
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nSecurity type from client: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(securityType[0]);
            Console.ForegroundColor = ConsoleColor.White;
#endif
            _socket.Send(securityType, securityType.Length, 0);

            byte[] securityHandshake = new byte[1];
            _socket.Receive(securityHandshake, securityHandshake.Length, 0);
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nSecurity handshake: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(securityHandshake[0]);
            Console.ForegroundColor = ConsoleColor.White;
#endif
            if (securityHandshake[0] != 0)
                _ExitProcessRetranslator("Security handshake error", 1);
        } catch (SocketException ex)
        {
            _ExitProcessRetranslator(ex.Message, 1);
        }
    }

    private void ClientInit()
    {
        byte[] sharedFlag = new byte[1] { 0 };
        try
        {
            _socket.Send(sharedFlag, sharedFlag.Length, 0);
        } catch (SocketException ex)
        {
            _ExitProcessRetranslator(ex.Message, 1);
        }
    }

    private void _ExitProcessRetranslator(string msg, int code)
    {
        Console.WriteLine(msg);
        Environment.Exit(code);
    }

    public void Connect()
    {
        makeHandshakes();
        ClientInit();
        ServerInit();
        SetEncoding();
    }
}
