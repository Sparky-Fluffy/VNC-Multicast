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

        string? ip = config.GetValue<string>("IP");
        int port = config.GetValue<int>("port");

        Retranslator client = new Retranslator(new byte[] { 127, 0, 0, 1 }, 5900
                , Encodings.Raw);
        client.Connect();

        await host.RunAsync();
    }

    static void Main(string[] args)
    {
        RunAppAsync(args);
    }
}
