using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace proxy;

class Program
{
    static async void RunAppAsync(string[] args)
    {
        using IHost host = Host.CreateApplicationBuilder(args).Build();
        IConfiguration config =
            host.Services.GetRequiredService<IConfiguration>();

        string? ip = config.GetValue<string?>("ip");
        int port = config.GetValue<int>("port");
        IPAddress ip_addr = null;

        if (ip == string.Empty || ip == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Строка параметра 'ip' не задана.");
            Console.ForegroundColor = ConsoleColor.White;
            Environment.Exit(1);
        } else
        {
            try
            {
                ip_addr = IPAddress.Parse(ip);
            } catch (FormatException fe)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(fe.Message);
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
#endif

        Retranslator client = new Retranslator(ip_addr, port, Encodings.Raw);
        client.Connect();
        client.FramebufferUpdateRequest();

        await host.RunAsync();
    }

    static void Main(string[] args)
    {
        RunAppAsync(args);
    }
}
