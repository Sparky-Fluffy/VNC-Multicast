using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace proxy_gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Run()
    {
    }

    private void StartProxyServer(object? sender, RoutedEventArgs e)
    {
#if DEBUG
        Console.WriteLine($"Server Address = {ServerAddressTb.Text}");
        Console.WriteLine("Group multicast address = " +
                $"{GroupMulticastAddressTb.Text}");
        Console.WriteLine($"Hide window = {isHideWinCheckBox.IsChecked}");
#endif
    }
}
