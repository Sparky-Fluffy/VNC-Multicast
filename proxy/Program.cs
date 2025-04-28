using System;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using RetranslatorLogics;

namespace proxy;

struct ConnectionData
{
    public string ServerIP { get; set; }
    public int ServerPort { get; set; }
    public string encoding { get; set; }
    public string multicastGroupIP { get; set; }
    public int multicastGroupPort { get; set; }
}

class Program
{
    static Retranslator client;

    static void WriteErrorAndExit(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        Environment.Exit(1);
    }

    static void RunAppAsync()
    {
        Console.CancelKeyPress += new ConsoleCancelEventHandler(ExitAppConsole);

        string json = File.ReadAllText("appsettings.json");
        var data = JsonConvert.DeserializeObject<ConnectionData>(json);

        IPAddress serverIPAddr = null;
        IPAddress multicastGroupIPAddr = null;
        Encodings enc = Encodings.Raw;

        if (data.ServerIP == string.Empty || data.ServerIP == null)
            WriteErrorAndExit("Строка параметра 'ip' не задана.");
        else if (data.encoding == string.Empty || data.encoding == null)
            WriteErrorAndExit("Кодировка не задана корректно.");
        else
        {
            try
            {
                serverIPAddr = IPAddress.Parse(data.ServerIP);
                multicastGroupIPAddr = IPAddress.Parse(data.multicastGroupIP);
                enc = (Encodings)Enum.Parse(typeof(Encodings), data.encoding);
            } catch (Exception e)
            {
                WriteErrorAndExit(e.Message);
            }
        }

        if (data.ServerPort < 5900 || data.ServerPort > 5906)
            WriteErrorAndExit("Неподходящий порт в appsettings.json.");
        else if (data.multicastGroupPort <= 1024)
            WriteErrorAndExit($"Указанный порт {data.multicastGroupPort} " +
                    "зарезервирован системой");

#if DEBUG
        Console.WriteLine($"Server IP = {data.ServerIP}.");
        Console.WriteLine($"Server port = {data.ServerPort}.");
        Console.WriteLine($"Encoding = {data.encoding}.");
        Console.WriteLine($"Encoding enum = {(byte)enc}");
        Console.WriteLine($"Multicast ip: {data.multicastGroupIP}");
        Console.WriteLine($"Multicast port: {data.multicastGroupPort}");
#endif

        if (enc != Encodings.Raw)
            WriteErrorAndExit("Нельзя выбрать кодировку не Raw.");

        client = new Retranslator(serverIPAddr, data.ServerPort, enc,
                multicastGroupIPAddr, data.multicastGroupPort);
        client.Connect();
#if !DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Соединение с сервером {data.ServerIP} " +
                "установлено...");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Для остановки работы нажмите Ctrl-C");
        Console.ForegroundColor = ConsoleColor.White;
#endif
        client.SetPixelFormat();

        ushort width = BitConverter.ToUInt16([client.width[1], client.width[0]]);
        ushort height = BitConverter.ToUInt16([client.height[1], client.height[0]]);
        ushort rectW = (ushort)(width / 10);
        ushort rectH = (ushort)(height / 10);

        while (true)
        {
            for (ushort x = 0; x < 10; x++)
            {
                for (ushort y = 0; y < 10; y++)
                {
                    client.FramebufferUpdateRequest(0, (ushort)(x * rectW), (ushort)(y * rectH), rectW, rectH);
                }
            }
        }
    }

    static void ExitAppConsole(object sender, ConsoleCancelEventArgs args)
    {
#if DEBUG
        Console.WriteLine("Завершаю приложение.");
#endif
        client.CloseAndFree();
#if DEBUG
        Console.WriteLine("Ресурсы освобождены");
#endif
    }

    static void Main(string[] args)
    {
        RunAppAsync();
    }
}
