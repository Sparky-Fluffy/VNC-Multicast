using System.Linq;
using System.Net.NetworkInformation;
using DynamicData;
using ReactiveUI;

namespace viewer.ViewModels;

public class NetIfaceManager : ReactiveObject
{

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

    private static NetworkInterface[] ifaces = null;
    
    public static int Count
    {
        get;
        private set;
    }

    private static NetIfaceManager? instance;
    public static ref NetIfaceManager Instance
    {
        get
        {
            if (instance == null) instance = new NetIfaceManager();
            return ref instance!;
        }
    }

    public void SetCurrent()
    {
        int index = GetIndexInList();
        SetInterface(index);
    }
    public void SetNext()
    {
        int index = GetIndexInList();
        SetInterface((index + 1) % Count);
    }

    private void SetInterface(int index)
    {
        if (ifaces == null) return;

        if (index > 0)
        {
            var iface = ifaces.ElementAt(index);
            NetIfaceName = iface.Name;
            NetIface = iface.GetIPProperties().GetIPv4Properties().Index;
        }
    }

    private int GetIndexInList()
    {
        ifaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where
            (
                i => i.SupportsMulticast &&
                i.Supports(NetworkInterfaceComponent.IPv4)
            ).ToArray();

        Count = ifaces.Count();
        return ifaces.IndexOf(ifaces.FirstOrDefault(i => i.Name == NetIfaceName));
    }
}
