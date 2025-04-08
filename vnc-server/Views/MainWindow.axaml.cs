using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace vnc_server.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void RunServer()
    {
#if DEBUG
        Console.WriteLine($"Close window: {isHideWin.IsChecked}");
        Console.WriteLine($"Port: {tbPortData.Text}");
        Console.WriteLine($"tbPortData.Text == string.Empty ({tbPortData.Text ==
            string.Empty})");
#endif
    }

    private void StartServer(object sender, RoutedEventArgs e)
    {
        RunServer();
    }
}
