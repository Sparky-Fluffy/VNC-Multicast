using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Base;

namespace vnc_server.Views;

public partial class MainWindow : Window
{
    public bool? hideWindow;
    public int port;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void SetParamsAsync()
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
        {
            await box.ShowWindowDialogAsync(this);
            Environment.Exit(1);
        }
    }

    private void RunServer()
    {
#if DEBUG
        Console.WriteLine($"Close window: {isHideWin.IsChecked}");
        Console.WriteLine($"Port: {tbPortData.Text}");
        Console.WriteLine($"PortData.Text == null ({tbPortData.Text == null})");
#endif
        SetParamsAsync();
    }

    private void StartServer(object sender, RoutedEventArgs e)
    {
        RunServer();
    }
}
