using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace vnc_server.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void StartServer(object sender, RoutedEventArgs e)
    {
        Console.WriteLine(isHideWin.IsChecked);
    }
}
