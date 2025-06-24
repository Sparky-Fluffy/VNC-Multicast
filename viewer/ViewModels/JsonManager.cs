using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace viewer.ViewModels;

public static class JsonManager
{
    public static JToken? TryParseJson(string path)
    {
        try { return JToken.Parse(File.ReadAllText(path)); }
        catch { return null; }
    }

    public static bool TryFetchAddresses(string path, out IList<AddressHolder> items)
    {
        items = new List<AddressHolder>();

        if (!File.Exists(path)) return false;

        JArray? addressList = (JArray?)TryParseJson(path);
        if (addressList == null || addressList.Count < 1) return false;

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

    public static bool TryFetchTranslations(string path, out Dictionary<string, string> items)
    {
        items = null;

        if (!File.Exists(path)) return false;

        JArray? translationList = (JArray?)TryParseJson(path);
        if (translationList == null || translationList?.Count < 1)
            return false;

        items = translationList
            .Where(t => t["Name"] != null && t["Value"] != null)
            ?.ToDictionary(t => (string)t["Name"], t => (string)t["Value"]);
        
        return items != null;
    }

    public static bool TryFetchSettings(string path, out Dictionary<string, string> items)
    {
        items = null;

        if (!File.Exists(path)) return false;

        JObject? settings = (JObject?)TryParseJson(path);
        if (settings == null || settings?.Count < 1)
            return false;

        items = settings.Children().ToDictionary
        (
            t => (t as JProperty).Name,
            t => (string)(t as JProperty).Value
        );
        
        return items != null;
    }

    public static void SaveSettings(string path, string lang, string theme, string netIface)
    {
        JObject? settings = null;

        if (!File.Exists(path))
        {
            FileStream? file = File.Create(path);
            file?.Close();
        }
        else settings = (JObject?)TryParseJson(path);

        if (settings == null) settings = new JObject();

        settings["Lang"] = lang;
        settings["Theme"] = theme;
        settings["NetInterface"] = netIface;

        File.WriteAllText(path, settings.ToString());
    }

    public static void Add(string path, string ip, ushort port)
    {
        JArray? addressList = null;

        if (!File.Exists(path))
        {
            FileStream? file = File.Create(path);
            file?.Close();
        }
        else addressList = (JArray?)TryParseJson(path);

        if (addressList == null) addressList = new JArray();

        addressList.Add
        (
            (JObject)JToken.FromObject(new AddressHolder { Ip = ip, Port = port })
        );

        File.WriteAllText(path, addressList.ToString());
    }

    public static void Delete(string path, object selected)
    {
        if (!File.Exists(path)) return;

        JArray? addressList = (JArray?)TryParseJson(path);

        if (addressList == null) return;

        JToken t = JToken.FromObject(selected);

        JToken? d = addressList.FirstOrDefault
        (
            item => (string?)item["Ip"] == (string)t["Ip"]! &&
                (ushort?)item["Port"] == (ushort)t["Port"]!
        );

        if (d != null) addressList.Remove(d);
        File.WriteAllText(path, addressList.ToString());
    }
}