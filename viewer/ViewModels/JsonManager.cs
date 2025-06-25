using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        return TryFetch<JArray, AddressHolder>(path, out items);
    }

    public static bool TryFetchLanguages(string path, out Dictionary<string, string> dict)
    {
        dict = new Dictionary<string, string>();
        if (TryFetch<JArray, LangHolder>(path, out var items) &&
            items != null && items.Count > 0)
            dict = items.ToDictionary(t => t.Name, t => t.Value);
        return dict != null;
    }

    public static bool TryFetchSettings(string path, out SettingsHolder settings)
    {
        settings = null;

        if (TryFetch<JObject, SettingsHolder>(path, out var items) &&
            items != null && items.Count > 0)
            settings = items[0];

        return settings != null;
    }

    public static bool TryFetch<TNode, TObj>(string path, out IList<TObj> items)
        where TNode : JToken
    {
        items = null;

        if (!File.Exists(path)) return false;

        TNode? node = (TNode?)TryParseJson(path);
        if (node == null || node.Count() < 1) return false;

        if (node is JObject)
            items = [(TObj)node.ToObject(typeof(TObj))!];
        else if (node is JArray)
            items =
            [..
                node?.Select
                (
                    a =>
                    {
                        try { return a.ToObject<TObj>(); }
                        catch { return (TObj)typeof(TObj).GetConstructor
            (
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[0],
                new ParameterModifier[0]
            ).Invoke(null); }
                    }
                )!
            ];

        return items != null;
    }
    
    public static void SaveSettings(string path, SettingsHolder settings) =>
        Save<JObject, SettingsHolder>(path, settings);

    public static void Add(string path, AddressHolder address) =>
        Save<JArray, AddressHolder>(path, address);

    public static void Save<TNode, TObj>(string path, TObj obj)
        where TNode : JToken where TObj : notnull
    {
        if (obj == null) return;
        TNode? node = null;

        if (!File.Exists(path))
        {
            FileStream? file = File.Create(path);
            file?.Close();
        }
        else node = (TNode?)TryParseJson(path);

        if (node == null)
            node = (TNode)typeof(TNode).GetConstructor
            (
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[0],
                new ParameterModifier[0]
            ).Invoke(null);

        if (node is JObject)
            node = (TNode)JToken.FromObject(obj);
        else if (node is JArray)
            (node as JArray)!.Add(JToken.FromObject(obj));
        File.WriteAllText(path, node.ToString());
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