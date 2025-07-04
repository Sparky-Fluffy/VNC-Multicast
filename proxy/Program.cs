﻿using System;
using System.Net;
using Newtonsoft.Json;
using System.IO;
using RetranslatorLogics;

namespace proxy;

public struct ConnectionData
{
    public string ServerIP { get; set; }
    public int ServerPort { get; set; }
    public string encoding { get; set; }
    public string multicastGroupIP { get; set; }
    public int multicastGroupPort { get; set; }
    public string localIP { get; set; }
    public int interfaceIndex { get; set; }
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
        IPAddress localIP = null;
        IPAddress multicastGroupIPAddr = null;
        Encodings enc = Encodings.Raw;

        if (data.ServerIP == string.Empty || data.ServerIP == null ||
                data.localIP == string.Empty || data.localIP == null)
            WriteErrorAndExit("Строка параметра 'ip' не задана.");
        else if (data.encoding == string.Empty || data.encoding == null)
            WriteErrorAndExit("Кодировка не задана корректно.");
        else if (data.interfaceIndex <= 0)
            WriteErrorAndExit("Интерфейс меньше, сука, блять, нуля, дебил.");
        else
        {
            try
            {
                serverIPAddr = IPAddress.Parse(data.ServerIP);
                multicastGroupIPAddr = IPAddress.Parse(data.multicastGroupIP);
                enc = (Encodings)Enum.Parse(typeof(Encodings), data.encoding);
                localIP = IPAddress.Parse(data.localIP);
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
                multicastGroupIPAddr, data.multicastGroupPort, localIP,
                data.interfaceIndex);
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

        ushort width = client.ScreenWidth;
        ushort height = client.ScreenHeight;

        while (true)
            client.FramebufferUpdateRequest(1, 0, 0, width, height);
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
