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
    public int port { get; }
    public IPAddress ip { get; }
    public Socket socket { get; }
    public Encodings encodingType { get; }
    public byte[] width { get; } = new byte[2];
    public byte[] height { get; } = new byte[2];
    public byte[] pixelFormat { get; } = new byte[16];

    public Retranslator(IPAddress ip, int port, Encodings encodingType)
    {
        this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
        this.ip = ip;
        this.port = port;
        this.encodingType = encodingType;
    }

    private void ServerInit()
    {
        try
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nServer Init message data: ");
            Console.ForegroundColor = ConsoleColor.Yellow;

            byte[] w = new byte[2];
            socket.Receive(w, w.Length, 0);

            width[0] = w[0];
            width[1] = w[1];

            foreach (byte s in width)
                Console.Write($"{s} ");

            byte[] h = new byte[2];
            socket.Receive(h, h.Length, 0);

            height[0] = h[0];
            height[1] = h[1];

            foreach (byte s in height)
                Console.Write($"{s} ");

            socket.Receive(pixelFormat, pixelFormat.Length, 0);
            foreach (byte s in pixelFormat)
                Console.Write($"{s} ");

            byte[] nameLenght = new byte[4];

            socket.Receive(nameLenght, nameLenght.Length, 0);
            foreach (byte s in nameLenght)
                Console.Write($"{s} ");
            
            Array.Reverse(nameLenght);
            int nameLenghtNumber = BitConverter.ToInt32(nameLenght, 0);

            byte[] nameString = new byte[nameLenghtNumber];
            socket.Receive(nameString, nameString.Length, 0);
            foreach (byte s in nameString)
                Console.Write($"{s} ");
            Console.WriteLine();
            
            Console.ForegroundColor = ConsoleColor.White;
        } catch (Exception e)
        {
            _ExitProcessRetranslator(e.Message, 1);
        }
    }

    public void SetPixelFormat()
    {
        try
        {
            byte[] fucked_msg = new byte[] {
                (byte)ClientMessageTypes.SetPixelFormat, 0, 0, 0,
                pixelFormat[0], pixelFormat[1], pixelFormat[2], pixelFormat[3],
                pixelFormat[4], pixelFormat[5], pixelFormat[6], pixelFormat[7],
                pixelFormat[8], pixelFormat[9], pixelFormat[10],
                pixelFormat[11], pixelFormat[12], pixelFormat[13],
                pixelFormat[14], pixelFormat[15] };
            socket.Send(fucked_msg, fucked_msg.Length, 0);
            Console.WriteLine("Установлен несчастный, сука, SexPixelFormat.");
        } catch (Exception e)
        {
            _ExitProcessRetranslator(e.Message, 1);
        }
    }

    public void FramebufferUpdateRequest(byte incremental = 0, ushort
            XPosition = 0, ushort YPosition = 0)
    {
        try
        {
            byte[] updateRequest = new byte[]
            { (byte)ClientMessageTypes.FramebufferUpdateRequest, incremental,
                (byte)(XPosition >> 8), (byte)XPosition, (byte)(YPosition >> 8),
                (byte)YPosition, width[0], width[1], height[0], height[1] };

#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nFrame buffer update request: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (byte b in updateRequest)
                Console.Write($"{b} ");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
#endif

            socket.Send(updateRequest, updateRequest.Length, 0);

            byte[] frameBufferUpdateMessageResponse = new byte[4];
            socket.Receive(frameBufferUpdateMessageResponse,
                    frameBufferUpdateMessageResponse.Length, 0);
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
            Console.ForegroundColor = ConsoleColor.White;
#endif
        } catch (Exception e)
        {
            _ExitProcessRetranslator(e.Message, 1);
        }
    }

    private void SetEncoding()
    {
        try
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"\nSet encoding message: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(encodingType);
            Console.ForegroundColor = ConsoleColor.White;
#endif
            byte[] msg = new byte[] { (byte)ClientMessageTypes.SetEncodings, 0,
                (byte)encodingType };
            socket.Send(msg, msg.Length, 0);
        } catch (Exception e)
        {
            _ExitProcessRetranslator(e.Message, 1);
        }
    }

    private void setProtocolVersion()
    {
        try
        {
            socket.Connect(ip, port);

            byte[] protocolVersion = new byte[12];
            socket.Receive(protocolVersion, protocolVersion.Length, 0);
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nProtocolVersion: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (byte p in protocolVersion)
                Console.Write($"{p} ");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
#endif
            socket.Send(protocolVersion, protocolVersion.Length, 0);
        } catch (Exception e)
        {
            _ExitProcessRetranslator(e.Message, 1);
        }
    }

    private byte[] getSecurityTypes()
    {
        byte[] numberOfSecurity = new byte[1];
        socket.Receive(numberOfSecurity, numberOfSecurity.Length, 0);

#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("\nNumber of security types: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{numberOfSecurity[0]}");
        Console.ForegroundColor = ConsoleColor.White;
#endif

        byte[] securityTypes = new byte[numberOfSecurity[0]];
        socket.Receive(securityTypes, securityTypes.Length, 0);

#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Security types: ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        foreach (var t in securityTypes)
            Console.Write($"{t} ");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
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
            Console.Write("\nSecurity type from client: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(securityType[0]);
            Console.ForegroundColor = ConsoleColor.White;
#endif
            socket.Send(securityType, securityType.Length, 0);

            byte[] securityHandshake = new byte[4];
            socket.Receive(securityHandshake, securityHandshake.Length, 0);
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"\nSecurity handshake: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var t in securityHandshake)
                Console.Write($"{t} ");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
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
            socket.Send(sharedFlag, sharedFlag.Length, 0);
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
