using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace viewer.ViewModels;

public class AddressHolder : ObservableObject
{
    private string ip = "";
    private ushort port = 0;

    [JsonProperty(Required = Required.Always)]
    public string Ip
    {
        get => ip;
        set => SetProperty(ref ip, value);
    }

    [JsonProperty(Required = Required.Always)]
    public ushort Port
    {
        get => port;
        set => SetProperty(ref port, value);
    }
}