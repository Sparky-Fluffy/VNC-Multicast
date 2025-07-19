using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
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
        return TryFetch(path, out items);
    }

    public static bool TryFetchLanguages(string path, out Dictionary<string, string> dict)
    {
        dict = new Dictionary<string, string>();
        if (TryFetch<LangHolder>(path, out var items) &&
            items != null && items.Count > 0)
            dict = items.ToDictionary(t => t.Name, t => t.Value);
        return dict != null;
    }

    public static bool TryFetchSettings(string path, out SettingsHolder settings)
    {
        settings = null;

        if (TryFetch<SettingsHolder>(path, out var items) &&
            items != null && items.Count > 0)
            settings = items[0];

        return settings != null;
    }

    public static bool TryFetch<TObj>(string path, out IList<TObj> items)
    {
        JsonSerializer serializer = new JsonSerializer();
        serializer.MissingMemberHandling = MissingMemberHandling.Error;
        serializer.NullValueHandling = NullValueHandling.Ignore;
        serializer.DefaultValueHandling = DefaultValueHandling.Ignore;
        items = null;

        if (!File.Exists(path)) return false;

        JToken? node = TryParseJson(path);
        if (node == null || node.Count() < 1) return false;

        if (node is JObject) items = [node.ToObject<TObj>()!];
        else if (node is JArray)
            items =
            [..
                node.Where
                (
                    a =>
                    {
                        try { return a.Count() > 0 && a.ToObject<TObj>(serializer) != null; }
                        catch { return false; }
                    }
                )
                .Select(a => a.ToObject<TObj>()!)
            ];

        return items != null;
    }
    
    public static void SaveSettings(string path, SettingsHolder settings) =>
        Save<JObject>(path, settings);

    public static void Add(string path, AddressHolder address) =>
        Save<JArray>(path, address);

    public static void Save<TNode>(string path, object obj) where TNode : JToken
    {
        if (obj == null) return;
        JToken? node = null;

        if (!File.Exists(path))
        {
            FileStream? file = File.Create(path);
            file?.Close();
        }
        
        if (typeof(TNode) == typeof(JObject))
            node = (TNode)JToken.FromObject(obj);

        else if (typeof(TNode) == typeof(JArray))
        {
            node = (JArray?)TryParseJson(path);
            node ??= new JArray();
            (node as JArray)?.Add(JToken.FromObject(obj));
        }
            
        File.WriteAllText(path, node!.ToString());
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
                (string?)item["Port"] == (((ushort)t["Port"]! == 0) ? "" : (string)t["Port"]!)
        );

        if (d != null) addressList.Remove(d);
        File.WriteAllText(path, addressList.ToString());
    }
}