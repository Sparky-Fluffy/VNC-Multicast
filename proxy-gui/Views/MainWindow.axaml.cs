using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using RetranslatorLogics;
using System.Net;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Base;

namespace proxy_gui.Views;

public partial class MainWindow : Window
{
    private IPAddress serverAddr;
    private IPAddress groupAddr;
    private bool? hideWin;
    private const uint minMulticastAddr = 3758096384;
    private const uint maxMulticastAddr = 4026531839;

    public MainWindow()
    {
        InitializeComponent();
    }

    private uint IP2Int(IPAddress groupIP)
    {
        byte[] bytes = groupIP.GetAddressBytes();

        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        return BitConverter.ToUInt32(bytes, 0);
    }

    private async Task SetAddresses()
    {
        string servAddrStr = ServerAddressTb.Text;
        string groupAddrStr = GroupMulticastAddressTb.Text;
        IMsBox<ButtonResult> box = null;

        if (string.IsNullOrEmpty(servAddrStr) ||
                string.IsNullOrEmpty(groupAddrStr))
        {
            box = MessageBoxManager.GetMessageBoxStandard("Пустое значение",
                    "Нет IP адреса сервера или группы", ButtonEnum.Ok);
        }
        else if (!IPAddress.TryParse(servAddrStr, out serverAddr) ||
            !IPAddress.TryParse(groupAddrStr, out groupAddr))
        {
            box = MessageBoxManager.GetMessageBoxStandard("Неверное значение",
                    "Неверные IP адреса", ButtonEnum.Ok);
        }
        else if (IP2Int(groupAddr) < minMulticastAddr || IP2Int(groupAddr) >
                maxMulticastAddr)
        {
            box = MessageBoxManager.GetMessageBoxStandard("Неверный диапазон",
                    "Адрес группы должен быть из диапазон 224.0.0.0 - " +
                    "239.255.255.255", ButtonEnum.Ok);
        }

        hideWin = isHideWinCheckBox.IsChecked;

        if (box != null)
            await box.ShowWindowDialogAsync(this);
    }

    private async void Run()
    {
        await SetAddresses();
    }

    private void StartProxyServer(object? sender, RoutedEventArgs e)
    {
#if DEBUG
        Console.WriteLine($"Server Address = {ServerAddressTb.Text}");
        Console.WriteLine("Group multicast address = " +
                $"{GroupMulticastAddressTb.Text}");
        Console.WriteLine($"Hide window = {isHideWinCheckBox.IsChecked}");
#endif
        Run();
    }
}
