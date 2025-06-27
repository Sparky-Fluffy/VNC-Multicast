using DynamicData;
using ReactiveUI;
using System.Linq;
using System.Net.NetworkInformation;

namespace viewer.ViewModels;

public class NetIfaceManager : SwitchManager<NetIfaceManager, NetworkInterface>
{
    private NetIfaceManager() {}

    protected override void SetItem(int index)
    {
        if (list == null || index < 0) return;

        var iface = list.ElementAt(index);
        NetIfaceName = iface.Name;
        NetIface = iface.GetIPProperties().GetIPv4Properties().Index;
    }

    public override void GetList()
    {
        list = NetworkInterface.GetAllNetworkInterfaces()
            .Where
            (
                i => i.SupportsMulticast &&
                i.Supports(NetworkInterfaceComponent.IPv4)
            ).ToArray();
        
        base.GetList();
    }

    protected override int GetIndex()
    {
        if (list == null) return -1;

        var item = list.FirstOrDefault(i => i.Name == NetIfaceName);
        return item != null ? list.IndexOf(item) : -1;
    }

    #region NETIFACE VARIABLES

    private int netIface = 0;
    public int NetIface
    {
        get => netIface;
        set => this.RaiseAndSetIfChanged(ref netIface, value);
    }

    private string netIfaceName;
    public string NetIfaceName
    {
        get => netIfaceName;
        set => this.RaiseAndSetIfChanged(ref netIfaceName, value);
    }
    
    #endregion
}
