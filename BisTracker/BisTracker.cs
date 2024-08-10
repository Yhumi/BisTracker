using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using BisTracker.UI;
using ECommons;
using ECommons.Automation.LegacyTaskManager;
using ECommons.DalamudServices;
using Dalamud.Interface.Style;
using BisTracker.RawInformation.Character;
using BisTracker.RawInformation;
using OtterGui.Classes;

namespace BisTracker;

public unsafe class BisTracker : IDalamudPlugin
{
    public string Name => "BisTracker";
    private const string CommandName = "/bis";

    internal static BisTracker P = null!;
    internal PluginUI PluginUi;
    internal WindowSystem ws;
    internal Configuration Config;
    internal TaskManager TM;
    internal TextureCache Icons;

    internal StyleModel Style;
    internal bool StylePushed = false;

    public BisTracker(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this, Module.All);
        P = this;

        LuminaSheets.Init();
        P.Config = Configuration.Load();
        TM = new();
        TM.TimeLimitMS = 1000;

        ws = new();
        ws.AddWindow(new MateriaMeldingUI());
        Icons = new(Svc.Data, Svc.Texture);
        Config = P.Config;
        PluginUi = new();

        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the BisTracker menu.\n" +
            "/bis settings → Opens settings.",
            ShowInHelp = true,
        });

        Svc.PluginInterface.UiBuilder.Draw += ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Svc.PluginInterface.UiBuilder.OpenMainUi += DrawConfigUI;

        Svc.ClientState.ClassJobChanged += OnCharacterJobChange;

        Style = StyleModel.GetFromCurrent()!;

        PluginUi.OpenWindow = OpenWindow.Bis;
    }

    public void Dispose()
    {
        PluginUi.Dispose();

        Svc.Commands.RemoveHandler(CommandName);
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Svc.PluginInterface.UiBuilder.Draw -= ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= DrawConfigUI;

        ws?.RemoveAllWindows();
        ws = null!;

        ECommonsMain.Dispose();
        P = null!;
    }

    private void OnCommand(string command, string args)
    {
        var subcommands = args.Split(' ');

        if (subcommands.Length == 0)
        {
            PluginUi.IsOpen = !PluginUi.IsOpen;
            return;
        }

        var firstArg = subcommands[0];

        CharacterInfo.UpdateCharaStats();

        // in response to the slash command, just toggle the display status of our main ui
        PluginUi.IsOpen = true;
        PluginUi.OpenWindow = firstArg.ToLower() switch
        {
            "settings" => OpenWindow.Settings,
            _ => OpenWindow.Bis
        };
    }

    private void DrawConfigUI()
    {
        PluginUi.IsOpen = true;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        CharacterInfo.UpdateCharaStats();
    }

    private void OnCharacterJobChange(uint classJobId)
    {
        CharacterInfo.UpdateCharaStats(classJobId);
    }
}
