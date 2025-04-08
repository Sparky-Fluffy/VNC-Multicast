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

    private void RunServer()
    {
#if DEBUG
        Console.WriteLine(isHideWin.IsChecked);
        Console.WriteLine(tbPortData.Text);
#endif
    }

    private void StartServer(object sender, RoutedEventArgs e)
    {
        RunServer();
    }
}
