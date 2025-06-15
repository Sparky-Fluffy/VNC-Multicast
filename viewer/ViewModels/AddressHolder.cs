using CommunityToolkit.Mvvm.ComponentModel;

namespace viewer.ViewModels;

public class AddressHolder : ObservableObject
{
    private string ip = "";
    private ushort port = 0;

    public string Ip
    {
        get => ip;
        set => SetProperty(ref ip, value);
    }

    public ushort Port
    {
        get => port;
        set => SetProperty(ref port, value);
    }
}