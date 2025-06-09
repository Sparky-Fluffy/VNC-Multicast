using CommunityToolkit.Mvvm.ComponentModel;

namespace viewer.ViewModels;

public class AddressHolder : ObservableObject
{
    private string name, ip;
    private ushort port;

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

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