using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RetranslatorLogics;

namespace proxy;

using FuckedExceptionKHSU = System.Exception;

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

    static async void RunAppAsync(string[] args)
    {
        Console.CancelKeyPress += new ConsoleCancelEventHandler(ExitAppConsole);

        using IHost host = Host.CreateApplicationBuilder(args).Build();
        IConfiguration config =
            host.Services.GetRequiredService<IConfiguration>();

        string? serverIP = config.GetValue<string?>("ServerIP");
        int serverPort = config.GetValue<int>("ServerPort");
        string? encoding = config.GetValue<string?>("encoding");
        string? multicastGroupIP = config.GetValue<string?>("multicastGroupIP");
        int multicastGroupPort = config.GetValue<int>("multicastGroupPort");

        IPAddress serverIPAddr = null;
        IPAddress multicastGroupIPAddr = null;
        Encodings enc = Encodings.Raw;

        if (serverIP == string.Empty || serverIP == null)
            WriteErrorAndExit("Строка параметра 'ip' не задана.");
        else if (encoding == string.Empty || encoding == null)
            WriteErrorAndExit("Кодировка не задана корректно.");
        else
        {
            try
            {
                serverIPAddr = IPAddress.Parse(serverIP);
                multicastGroupIPAddr = IPAddress.Parse(multicastGroupIP);
                enc = (Encodings)Enum.Parse(typeof(Encodings), encoding);
            } catch (FuckedExceptionKHSU e)
            {
                WriteErrorAndExit(e.Message);
            }
        }

        if (serverPort < 5900 || serverPort > 5906)
            WriteErrorAndExit("Неподходящий порт в appsettings.json.");
        else if (multicastGroupPort <= 1024)
            WriteErrorAndExit($"Указанный порт {multicastGroupPort} " +
                    "зарезервирован системой");

#if DEBUG
        Console.WriteLine($"Server IP = {serverIP}.");
        Console.WriteLine($"Server port = {serverPort}.");
        Console.WriteLine($"Encoding = {encoding}.");
        Console.WriteLine($"Encoding enum = {(byte)enc}");
        Console.WriteLine($"Multicast ip: {multicastGroupIP}");
        Console.WriteLine($"Multicast port: {multicastGroupPort}");
#endif

        if (enc != Encodings.Raw)
            WriteErrorAndExit("Нельзя выбрать кодировку не Raw.");

        client = new Retranslator(serverIPAddr, serverPort, enc,
                multicastGroupIPAddr, multicastGroupPort);
        client.Connect();
        client.SetPixelFormat();
        while (true)
            client.FramebufferUpdateRequest();

        await host.RunAsync();
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
        RunAppAsync(args);
    }
}
