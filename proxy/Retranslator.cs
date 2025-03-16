using System;
using System.Net;
using System.Net.Sockets;

namespace proxy;

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
    public int _port { get; private set; }
    public IPAddress _ip { get; private set; }
    public Socket _socket { get; private set; }
    public Encodings _encodingType { get; private set; }

    public Retranslator(byte[] addr, int port, Encodings encodingType)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
        _ip = new IPAddress(addr);
        _port = port;
        _encodingType = encodingType;
    }

    private void ServerInit()
    {
        byte[] serverInfo = new byte[64];
        _socket.Receive(serverInfo, serverInfo.Length, 0);

#if DEBUG
        Console.WriteLine("\nServer Init message data");
        foreach (byte s in serverInfo)
            Console.Write($"{s} ");
        Console.WriteLine();
#endif
    }

    private void SetPixelFormat()
    {
    }

    private void FramebufferUpdateRequest()
    {
    }

    private void KeyEvent()
    {
    }

    private void PointerEvent()
    {
    }

    private void ClientCutText()
    {
    }

    private void SetEncoding()
    {
#if DEBUG
        Console.WriteLine($"\nSet encoding message: {_encodingType}");
#endif
        byte[] msg = new byte[] { (byte)ClientMessageTypes.SetEncodings, 0,
            (byte)_encodingType };
        _socket.Send(msg, msg.Length, 0);
    }

    private void makeHandshakes()
    {
        try
        {
#if DEBUG
            Console.WriteLine("Попытка подключения к серверу...");
#endif
            _socket.Connect(_ip, _port);

            byte[] protocolVersion = new byte[12];
            _socket.Receive(protocolVersion, protocolVersion.Length, 0);
#if DEBUG
            Console.WriteLine("\nProtocolVersion");
            foreach (byte p in protocolVersion)
                Console.Write($"{p} ");
            Console.WriteLine();
#endif
            _socket.Send(protocolVersion, protocolVersion.Length, 0);

            byte[] securityTypes = new byte[2];
            _socket.Receive(securityTypes, securityTypes.Length, 0);
#if DEBUG
            Console.WriteLine("\nSecurity Types");
            foreach (byte st in securityTypes)
                Console.Write($"{st} ");
            Console.WriteLine();
#endif

            byte[] securityType = new byte[] { securityTypes[0] };
#if DEBUG
            Console.WriteLine($"\nSecurity type from client: {securityType[0]}");
#endif
            _socket.Send(securityType, securityType.Length, 0);

            byte[] securityHandshake = new byte[1];
            _socket.Receive(securityHandshake, securityHandshake.Length, 0);
#if DEBUG
            Console.WriteLine($"\nSecurity handshake: {securityHandshake[0]}.");
#endif
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
