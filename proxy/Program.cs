using System.Net;
using System.Net.Sockets;

namespace proxy;

class Retranslator
{
    public int port { get; private set; }
    public IPAddress ip { get; private set; }
    public Socket socket { get; private set; }

    public Retranslator()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
    }

    private void LogMessage(ConsoleColor color, string msg)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
    }

    public void ClientInit()
    {
        byte[] sharedFlag = new byte[1] { 0 };
        socket.Send(sharedFlag, sharedFlag.Length, 0);
    }

    public void Connect(byte[] addr, int port)
    {
        ip = new IPAddress(addr);
        try
        {
            socket.Connect(ip, port);

            byte[] protocolVersion = new byte[12];
            socket.Receive(protocolVersion, protocolVersion.Length, 0);

            socket.Send(protocolVersion, protocolVersion.Length, 0);

            byte[] securityTypes = new byte[2];
            socket.Receive(securityTypes, securityTypes.Length, 0);

            byte[] securityType = new byte[] { securityTypes[0] };
            socket.Send(securityType, securityType.Length, 0);

            byte[] securityHandshake = new byte[1];
            socket.Receive(securityHandshake, securityHandshake.Length, 0);

            switch (securityHandshake[0])
            {
                case 0:
                    LogMessage(ConsoleColor.Green, "Connected...");
                    break;
                case 1:
                    LogMessage(ConsoleColor.Red, "Not connected...");
                    break;
            }
            Console.ForegroundColor = ConsoleColor.White;
        } catch (SocketException ex)
        {
            LogMessage(ConsoleColor.Red, ex.Message);
        } finally
        {
            socket.Dispose();
            socket.Close();
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Retranslator retranslator = new Retranslator();
        retranslator.Connect(new byte[] { 127, 0, 0, 1 }, 5900);
        retranslator.ClientInit();
    }
}
