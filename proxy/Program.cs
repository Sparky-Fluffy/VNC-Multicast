using System;
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

        if (ip == string.Empty || ip == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Неверный ip.");
            Console.ForegroundColor = ConsoleColor.White;
            Environment.Exit(1);
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

        Retranslator client = new Retranslator(ip!, port, Encodings.Raw);
        client.Connect();
        client.FramebufferUpdateRequest(0, 15, 15, 100, 100);

        await host.RunAsync();
    }

    static void Main(string[] args)
    {
        RunAppAsync(args);
    }
}
