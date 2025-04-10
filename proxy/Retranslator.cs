using System;
using System.Net;
using System.Net.Sockets;

namespace proxy;

using FuckedExceptionKHSU = System.Exception;

enum CloseProxyStatus : byte
{
    SuccessYEAH = 0,
    FailedSuck = 1
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

    public Retranslator(IPAddress ip, int port, Encodings encodingType)
    {
        this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
        this.ip = ip;
        this.port = port;
        this.encodingType = encodingType;
        multicastSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
        multicastSocket.EnableBroadcast = true;
    }

    private void ServerInit()
    {
        try
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nServer Init message data: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
#endif

            byte[] w = new byte[2];
            socket.Receive(w, w.Length, 0);

            width[0] = w[0];
            width[1] = w[1];

#if DEBUG
            foreach (byte s in width)
                Console.Write($"{s} ");
#endif

            byte[] h = new byte[2];
            socket.Receive(h, h.Length, 0);

            height[0] = h[0];
            height[1] = h[1];

#if DEBUG
            foreach (byte s in height)
                Console.Write($"{s} ");
#endif

            socket.Receive(pixelFormat, pixelFormat.Length, 0);

#if DEBUG
            foreach (byte s in pixelFormat)
                Console.Write($"{s} ");
#endif

            byte[] nameLenght = new byte[4];

            socket.Receive(nameLenght, nameLenght.Length, 0);

#if DEBUG
            foreach (byte s in nameLenght)
                Console.Write($"{s} ");
#endif
            
            Array.Reverse(nameLenght);
            int nameLenghtNumber = BitConverter.ToInt32(nameLenght, 0);

            byte[] nameString = new byte[nameLenghtNumber];
            socket.Receive(nameString, nameString.Length, 0);

#if DEBUG
            foreach (byte s in nameString)
                Console.Write($"{s} ");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
#endif
        } catch (FuckedExceptionKHSU e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.FailedSuck);
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
        } catch (FuckedExceptionKHSU e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.FailedSuck);
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

            byte[] countRects = new byte[4];
            socket.Receive(countRects, countRects.Length, 0);
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\nFrame Buffer Update Message Response: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (byte s in countRects)
                Console.Write($"{s} ");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
#endif

            ushort numberOfRectangles = BitConverter.ToUInt16([countRects[3],
                    countRects[2]], 0);

            byte[] rectData = new byte[12];
            byte[] pixelData = new byte[pixelFormat[0] / 8];

            for (ushort i = 0; i < numberOfRectangles; i++)
            {
                socket.Receive(rectData, rectData.Length, 0);
                ushort width = BitConverter.ToUInt16([rectData[5], rectData[4]]);
                ushort height = BitConverter.ToUInt16([rectData[7], rectData[6]]);
#if DEBUG
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("\nRectangle header: ");

                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (byte s in rectData)
                    Console.Write($"{s} ");
                
                Console.WriteLine();
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Rectangle width and height: ");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{width}x{height}");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Rectangle pixel data is in prostration");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("В дело вступает мультикаст сокет.");
#endif
                for (int j = 0; j < width * height; j++)
                {
                    socket.Receive(pixelData, pixelData.Length, 0);
                }
            }
        } catch (FuckedExceptionKHSU e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.FailedSuck);
        }
    }

    private void SetEncoding()
    {
        try
        {
            byte[] numberOfEncodings = new byte[] {
                (byte)ClientMessageTypes.SetEncodings, 0, 0, 1 };
            socket.Send(numberOfEncodings, numberOfEncodings.Length, 0);

            byte[] encodingTypeMsg = new byte[] { 0, 0, 0, (byte)encodingType };
            socket.Send(encodingTypeMsg, encodingTypeMsg.Length, 0);
        } catch (FuckedExceptionKHSU e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.FailedSuck);
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
        } catch (FuckedExceptionKHSU e)
        {
            ExitProcessRetranslator(e.Message, CloseProxyStatus.FailedSuck);
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
        } catch (FuckedExceptionKHSU ex)
        {
            ExitProcessRetranslator(ex.Message, CloseProxyStatus.FailedSuck);
        }
    }

    private void ClientInit()
    {
        byte[] sharedFlag = new byte[1] { 0 };
        try
        {
            socket.Send(sharedFlag, sharedFlag.Length, 0);
        } catch (FuckedExceptionKHSU ex)
        {
            ExitProcessRetranslator(ex.Message, CloseProxyStatus.FailedSuck);
        }
    }

    private void ExitProcessRetranslator(string msg, CloseProxyStatus st)
    {
        Console.WriteLine(msg);
        Environment.Exit((int)st);
    }

    public void Connect()
    {
        makeHandshakes();
        ClientInit();
        ServerInit();
        SetEncoding();
    }
}
