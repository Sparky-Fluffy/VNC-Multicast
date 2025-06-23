using System.Linq;
using System.Net.NetworkInformation;
using DynamicData;

namespace viewer.ViewModels;

public static class NetIfaceManager
{
    public static int Count
    {
        get;
        private set;
    }

    private static NetworkInterface[] interfaces = null;

    public static int GetIndexInList(string interfaceName)
    {
        interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where
            (
                i => i.SupportsMulticast &&
                i.Supports(NetworkInterfaceComponent.IPv4)
            ).ToArray();

        Count = interfaces.Count();

        return interfaces.IndexOf(interfaces.FirstOrDefault(i => i.Name == interfaceName));
    }

    public static (string, int) GetNameAndIndex(int index)
    {
        if (interfaces == null) return ("Error", 0);

        var iface = interfaces.ElementAt(index);
        return (iface.Name, iface.GetIPProperties().GetIPv4Properties().Index);
    }
}