using System.Net;
using System.Net.Sockets;

namespace proxy;

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

        foreach (byte s in serverInfo)
            Console.WriteLine(s);
    }

    private void SetEncoding()
    {
        byte[] msg = new byte[] { 2, 0, (byte)_encodingType };
        _socket.Send(msg, msg.Length, 0);
    }

    private void makeHandshakes()
    {
        try
        {
            _socket.Connect(_ip, _port);

            byte[] protocolVersion = new byte[12];
            _socket.Receive(protocolVersion, protocolVersion.Length, 0);

            _socket.Send(protocolVersion, protocolVersion.Length, 0);

            byte[] securityTypes = new byte[2];
            _socket.Receive(securityTypes, securityTypes.Length, 0);

            byte[] securityType = new byte[] { securityTypes[0] };
            _socket.Send(securityType, securityType.Length, 0);

            byte[] securityHandshake = new byte[1];
            _socket.Receive(securityHandshake, securityHandshake.Length, 0);
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

class Program
{
    static void Main(string[] args)
    {
        Retranslator client = new Retranslator(new byte[] { 127, 0, 0, 1 },
                5900, Encodings.Raw);
        client.Connect();
    }
}
