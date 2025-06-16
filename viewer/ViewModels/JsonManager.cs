using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace viewer.ViewModels;

public static class JsonManager
{
    public static JObject? TryParseJson(string path)
    {
        try { return JObject.Parse(File.ReadAllText(path)); }
        catch { return null; }
    }

    public static bool TryFetchAddresses(string path, out IList<AddressHolder> items)
    {
        items = new List<AddressHolder>();

        if (!File.Exists(path)) return false;

        JObject? mainNode = null;
        mainNode = TryParseJson(path);
        if (mainNode == null) return false;

        JArray? addressList = (JArray?)mainNode["addr-list"];
        if (addressList == null) return false;

        if (addressList?.Count < 1) return false;

        items = new List<AddressHolder>
        (
            addressList
            .Where(a => a["Ip"] != null && a["Port"] != null)
            .Select
            (
                a => new AddressHolder
                {
                    Ip = (string)a["Ip"],
                    Port = (ushort)a["Port"],
                }
            )
        );
        return true;
    }

    public static void Add(string path, IPAddress ip, ushort port)
    {
        JObject? mainNode = null;
        JArray? addressList = null;

        FileStream? file = null;

        if (!File.Exists(path))
        {
            file = File.Create(path);
            file?.Close();
        }

        else mainNode = TryParseJson(path);

        if (mainNode == null) mainNode = new JObject();
        else addressList = (JArray?)mainNode["addr-list"];

        if (addressList == null) addressList = new JArray();

        addressList.Add
        (
            (JObject)JToken.FromObject
            (
                new AddressHolder
                {
                    Ip = ip.ToString(),
                    Port = port
                }
            )
        );

        mainNode["addr-list"] = addressList;

        File.WriteAllText(path, mainNode.ToString());
    }

    public static void Delete(string path, object selected)
    {
        if (File.Exists(path))
        {
            JObject? mainNode = null;
            JArray? addressList = null;
            mainNode = JsonManager.TryParseJson(path);
            addressList = (JArray?)mainNode?["addr-list"];
            if (addressList != null)
            {
                JToken t = JToken.FromObject(selected);
                int index = -1;
                for (int i = 0; i < addressList.Count; i++)
                {
                    if ((string?)addressList[i]["Ip"] == (string)t["Ip"] &&
                        (ushort?)addressList[i]["Port"] == (ushort)t["Port"])
                    {
                        index = i;
                        break;
                    }
                }

                addressList.RemoveAt(index);
                mainNode["addr-list"] = addressList;
                File.WriteAllText(path, mainNode.ToString());
            }
        }
    }
}