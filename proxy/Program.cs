using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace proxy;

class Program
{
    static void WriteErrorAndExit(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;
        Environment.Exit(1);
    }

    static async void RunAppAsync(string[] args)
    {
        using IHost host = Host.CreateApplicationBuilder(args).Build();
        IConfiguration config =
            host.Services.GetRequiredService<IConfiguration>();

        string? ip = config.GetValue<string?>("ip");
        int port = config.GetValue<int>("port");
        string? encoding = config.GetValue<string?>("encoding");

        IPAddress ip_addr = null;
        Encodings enc = Encodings.Raw;

        if (ip == string.Empty || ip == null)
            WriteErrorAndExit("Строка параметра 'ip' не задана.");
        else if (encoding == string.Empty || encoding == null)
            WriteErrorAndExit("Кодировка не задана корректно.");
        else
        {
            try
            {
                ip_addr = IPAddress.Parse(ip);
                enc = (Encodings)Enum.Parse(typeof(Encodings), encoding);
            } catch (FormatException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(1);
            }
        }

        if (port < 5900 || port > 5906)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Неподходящий порт: {port}.");
            Console.ForegroundColor = ConsoleColor.White;
            Environment.Exit(1);
        }

#if DEBUG
        Console.WriteLine($"ip = {ip}.");
        Console.WriteLine($"port = {port}.");
        Console.WriteLine($"Encoding = {encoding}.");
        Console.WriteLine($"Encoding enum = {(byte)enc}");
#endif

        Retranslator client = new Retranslator(ip_addr, port, enc);
        client.Connect();
        client.FramebufferUpdateRequest();

        await host.RunAsync();
    }

    static void Main(string[] args)
    {
        RunAppAsync(args);
    }
}
