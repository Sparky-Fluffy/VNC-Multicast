using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Base;
using Avalonia.Headless.Vnc;

namespace vnc_server.Views;

public partial class MainWindow : Window
{
    private bool? hideWindow;
    private int port;
    private byte[] protocolVersion38 = new byte[12] { 82, 70, 66, 32, 48, 48,
                                                    51, 46, 48, 48, 56, 10 };
    private Socket socket;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeSocket()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                ProtocolType.Tcp);
    }

    private async Task SetParamsAsync()
    {
        IMsBox<ButtonResult> box = null;
        hideWindow = isHideWin.IsChecked;

        if (tbPortData.Text == null)
            box = MessageBoxManager.GetMessageBoxStandard("Пустое значение",
                    "Не введён порт для запуска", ButtonEnum.Ok);
        else if (int.TryParse(tbPortData.Text, out port) && (port < 5900 ||
                    port > 5906))
            box = MessageBoxManager.GetMessageBoxStandard("Неверный порт",
                    "Порт должен быть от 5900 до 5906", ButtonEnum.Ok);
        if (box != null)
            await box.ShowWindowDialogAsync(this);
#if DEBUG
        Console.WriteLine($"Variables: hideWindow = {hideWindow}, port = " +
                $"{port}");
#endif
    }

    private async void PrepareData()
    {
#if DEBUG
        Console.WriteLine($"Close window: {isHideWin.IsChecked}");
        Console.WriteLine($"Port: {tbPortData.Text}");
        Console.WriteLine($"PortData.Text == null ({tbPortData.Text == null})");
#endif
        await SetParamsAsync();
    }

    private void RunServer(bool? isHide, int port)
    {
#if DEBUG
        Console.WriteLine($"RunServer: port = {port}");
#endif
        if (isHide.Value) Hide();
        else bStart.Content = "Остановить";
    }

    private void StopServer()
    {
#if DEBUG
        Console.WriteLine("Остановка сервера");
#endif
        bStart.Content = "Старт";
        tbPortData.Text = null;
        isHideWin.IsChecked = false;
    }

    private void StartServer(object sender, RoutedEventArgs e)
    {
        // if (bStart.Content == "Старт")
        // {
        //     PrepareData();
        //     if (port >= 5900 && port <= 5906)
        //         RunServer(hideWindow, port);
        // } else if (bStart.Content == "Остановить")
        //     StopServer();
    }
}
