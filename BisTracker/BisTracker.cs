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
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Game.Inventory;
using System.Collections.Generic;
using BisTracker.BiS;
using System.Linq;
using System;
using BisTracker.Melding;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentPartyMember.Delegates;
using ECommons.Schedulers;

namespace BisTracker;

public unsafe class BisTracker : IDalamudPlugin
{
    public string Name => "BisTracker";
    private const string CommandName = "/bis";
    private const int CurrentConfigVersion = 2;

    internal static BisTracker P = null!;
    internal PluginUI PluginUi;
    internal WindowSystem ws;
    internal Configuration Config;
    internal TaskManager TM;
    internal TextureCache Icons;

    internal MateriaMeldingUI MeldUI;

    internal StyleModel Style;
    internal bool StylePushed = false;

    public BisTracker(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this, Module.All);
        P = this;

        LuminaSheets.Init();
        ConstantData.Init();
        P.Config = Configuration.Load();

        TM = new() { AbortOnTimeout = true, TimeLimitMS = 20000 };

        if (P.Config.Version != CurrentConfigVersion)
        {
            P.Config.UpdateConfig();
        }

        CharacterInfo.SetCharaInventoryPointers();

        ws = new();
        MeldUI = new MateriaMeldingUI();
        ws.AddWindow(MeldUI);
        Icons = new(Svc.Data, Svc.Texture);
        Config = P.Config;
        PluginUi = new();

        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the BisTracker menu.\n" +
            "/bis settings → Opens settings.",
            ShowInHelp = true,
        });

        AutoMeld.Init();

        Svc.PluginInterface.UiBuilder.Draw += ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += DrawSettingsUI;
        Svc.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;

        Svc.ClientState.Login += OnClientLogin;
        Svc.ClientState.Logout += OnClientLogout;
        Svc.ClientState.ClassJobChanged += OnCharacterJobChange;
        Svc.GameInventory.InventoryChanged += OnInventoryChange;

        Svc.Framework.Update += Tick;

        Style = StyleModel.GetFromCurrent()!;

        CharacterInfo.UpdateCharaStats();
    }

    public void Dispose()
    {
        PluginUi.Dispose();

        Svc.Commands.RemoveHandler(CommandName);
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= DrawSettingsUI;
        Svc.PluginInterface.UiBuilder.Draw -= ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi -= DrawMainUI;

        Svc.ClientState.Login -= OnClientLogin;
        Svc.ClientState.Logout -= OnClientLogout;
        Svc.ClientState.ClassJobChanged -= OnCharacterJobChange;
        Svc.GameInventory.InventoryChanged -= OnInventoryChange;

        Svc.Framework.Update -= Tick;

        EquipGear.Dispose();

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

    private void DrawMainUI()
    {
        CharacterInfo.UpdateCharaStats();
        PluginUi.OpenWindow = OpenWindow.Bis;
        PluginUi.IsOpen = true;
    }

    private void DrawSettingsUI()
    {
        CharacterInfo.UpdateCharaStats();
        PluginUi.OpenWindow = OpenWindow.Settings;
        PluginUi.IsOpen = true;
    }

    private void Tick(object _)
    {
        AutoMeld.Tick();
        EquipGear.Update();
    }

    private void OnCharacterJobChange(uint classJobId)
    {
        CharacterInfo.UpdateCharaStats(classJobId);
    }

    private void OnClientLogin()
    {
        CharacterInfo.SetCharaInventoryPointers();
        CharacterInfo.UpdateCharaStats();
        BiSUI.ResetInputs();
        BiSUI.ResetBis();
    }

    private void OnClientLogout(int type, int code)
    {
        BiSUI.ResetInputs();
        BiSUI.ResetBis();
        PluginUi.IsOpen = false;
        PluginUi.OpenWindow = OpenWindow.None;
    }

    private void OnInventoryChange(IReadOnlyCollection<InventoryEventArgs> events)
    {
        if (events.Any(x => x.Type == GameInventoryEvent.Added || x.Type == GameInventoryEvent.Removed || x.Type == GameInventoryEvent.Moved))
        {
            BiSUI.UpdateItemCheck();
        }
    }
}
