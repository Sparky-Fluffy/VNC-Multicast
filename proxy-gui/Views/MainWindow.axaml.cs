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
    private int portConnection;
    private bool? hideWin;
    private Encodings encoding;
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

    private async Task<bool> SetAddresses()
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
        } else if (!int.TryParse(ServerPortTb.Text, out portConnection))
        {
            box = MessageBoxManager.GetMessageBoxStandard("Некорректный порт",
                    "Порт введён неправильно", ButtonEnum.Ok);
        }  else if (portConnection < 5900 || portConnection > 5906)
        {
            box = MessageBoxManager.GetMessageBoxStandard("Недействительный " +
                    "диапазон", "Порт принимает значения от 5900 до 5906",
                    ButtonEnum.Ok);
        }

        hideWin = isHideWinCheckBox.IsChecked;

        if (box != null)
        {
            await box.ShowWindowDialogAsync(this);
            return false;
        }
        return true;
    }

    private async void RunAppAsync()
    {
        if (!await SetAddresses()) return;

        if (hideWin.Value) Hide();
        else
        {
            bStart.IsEnabled = false;
            bStop.IsEnabled = true;
        }

        Retranslator retranslator = new Retranslator(serverAddr, portConnection,
                Encodings.Raw, groupAddr);

        await Task.Run(() =>
        {
            retranslator.Connect();
            retranslator.FramebufferUpdateRequest();
        });
    }

    private void StartProxyServer(object? sender, RoutedEventArgs e)
    {
#if DEBUG
        Console.WriteLine($"Server Address = {ServerAddressTb.Text}");
        Console.WriteLine("Group multicast address = " +
                $"{GroupMulticastAddressTb.Text}");
        Console.WriteLine($"Hide window = {isHideWinCheckBox.IsChecked}");
#endif
        RunAppAsync();
    }

    private void StopProxyServer(object? sender, RoutedEventArgs e)
    {
        bStart.IsEnabled = true;
        bStop.IsEnabled = false;
    }
}
