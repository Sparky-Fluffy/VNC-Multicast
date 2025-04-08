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
        Console.WriteLine(isHideWin.IsChecked);
        Console.WriteLine(tbPortData.Text);
#endif
        var box = MessageBoxManager
            .GetMessageBoxStandard("Caption", "Are you sure you would like to" +
                    "delete appender_replace_page_1?",
                ButtonEnum.YesNo);
        box.ShowAsync();
    }

    private void StartServer(object sender, RoutedEventArgs e)
    {
        RunServer();
    }
}
