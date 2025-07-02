using System;
using CommandLine;
using viewer.ViewModels;

namespace viewer.ConsoleApp;

[Verb("add", aliases: ["a"], HelpText = "Add new address to list.")]
internal class AddOptions : AddressOptions;

[Verb("delete", aliases: ["d", "del"], HelpText = "Delete address from list.")]
internal class DeleteOptions : AddressOptions;

internal class AddressOptions : AddressHolder
{
    [Option('a', "address", Required = true, HelpText = "IP-address")]
    public new string Ip
    {
        get => base.Ip;
        set => base.Ip = value;
    }

    [Option('p', "port", Required = true, HelpText = "Port")]
    public new ushort Port
    {
        get => base.Port;
        set => base.Port = value;
    }
}

[Verb("set", aliases: ["s", "st"], HelpText = "Change app settings.")]
internal class SettingsOptions : SettingsHolder
{
    [Option('l', "language", HelpText = "Application language")]
    public new string Lang
    {
        get => base.Lang;
        set => base.Lang = value;
    }

    [Option('t', "theme", HelpText = "Application theme")]
    public new string Theme
    {
        get => base.Theme;
        set => base.Theme = value;
    }

    [Option('i', "interface", HelpText = "Network interface")]
    public new string NetInterface
    {
        get => base.NetInterface;
        set => base.NetInterface = value;
    }
}

public static class ConsoleApplication
{
    public static void Run(string[] args)
    {
        int res = Parser.Default.ParseArguments<AddOptions, DeleteOptions, SettingsOptions>(args)
            .MapResult
            (
                (AddOptions opts) => RunAdd(opts),
                (DeleteOptions opts) => RunDelete(opts),
                (SettingsOptions opts) => RunSettings(opts),
                errors => 1
            );
        Environment.Exit(res);
    }

    private static int RunAdd(AddOptions opts)
    {
        JsonManager.Add(Paths.Address, opts);
        return 0;
    }

    private static int RunDelete(DeleteOptions opts)
    {
        JsonManager.Delete(Paths.Address, opts);
        return 0;
    }

    private static int RunSettings(SettingsOptions opts)
    {
        if (JsonManager.TryFetchSettings(Paths.Settings, out var settings))
        {
            if (opts.Lang == "") opts.Lang = settings.Lang;
            if (opts.Theme == "") opts.Theme = settings.Theme;
            if (opts.NetInterface == "") opts.NetInterface = settings.NetInterface;
        }
        JsonManager.SaveSettings(Paths.Settings, opts);
        return 0;
    }
}