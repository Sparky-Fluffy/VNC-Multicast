using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Base;
using Avalonia.Headless.Vnc;

namespace vnc_server.Views;

public partial class MainWindow : Window
{
    public bool? hideWindow;
    public int port;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async Task SetParamsAsync()
    {
        IMsBox<ButtonResult> box = null;
        hideWindow = isHideWin.IsChecked;

        if (tbPortData.Text == null)
            box = MessageBoxManager.GetMessageBoxStandard("Пустое значение",
                    "Не введён порт для запуска", ButtonEnum.Ok);
        else
        {
            int.TryParse(tbPortData.Text, out port);
            if (port < 5900 || port > 5906)
                box = MessageBoxManager.GetMessageBoxStandard("Неверный порт",
                    "Порт должен быть от 5900 до 5906", ButtonEnum.Ok);
        }
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
    }

    private void StartServer(object sender, RoutedEventArgs e)
    {
        PrepareData();
        if (port >= 5900 && port <= 5906)
            RunServer(hideWindow, port);
    }
}
