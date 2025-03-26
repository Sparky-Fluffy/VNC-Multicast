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

        Retranslator client = new Retranslator(ip, port, Encodings.Raw);
        client.Connect();

        await host.RunAsync();
    }

    static void Main(string[] args)
    {
        RunAppAsync(args);
    }
}
