using ECommons;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Interface.Utility.Raii;
using System;
using ECommons.DalamudServices;
using System.IO;
using ECommons.ImGuiMethods;
using System.Numerics;
using BisTracker.BiS;
using Dalamud.Interface.Components;
using Dalamud.Utility;

namespace BisTracker.UI;

internal unsafe class PluginUI : Window
{
    private bool visible = false;
    public OpenWindow OpenWindow { get; set; }

    public bool Visible
    {
        get { return this.visible; }
        set { this.visible = value; }
    }

    public PluginUI() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###BisTracker")
    {
        this.RespectCloseHotkey = false;
        this.SizeConstraints = new()
        {
            MinimumSize = new(250, 100),
            MaximumSize = new(9999, 9999)
        };
        P.ws.AddWindow(this);
    }

    public override void PreDraw()
    {
        //P.Style.Push();
        //P.StylePushed = true;
    }

    public override void PostDraw()
    {
        //if (P.StylePushed)
        //{
        //    P.Style.Pop();
        //    P.StylePushed = false;
        //}
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;
        var topLeftSideHeight = region.Y;

        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f.Scale(), 0));
        try
        {
            using (var table = ImRaii.Table($"BisTrackerTableContainer", 2, ImGuiTableFlags.Resizable))
            {
                if (!table)
                    return;

                ImGui.TableSetupColumn("##LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);
                ImGui.TableNextColumn();

                var regionSize = ImGui.GetContentRegionAvail();
                using (var leftChild = ImRaii.Child($"###BisTrackerLeftSide", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                {
                    var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Images/dog.png");

                    if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
                    {
                        ImGuiEx.LineCentered("###BisTrackerLogo", () =>
                        {
                            ImGui.Image(logo.ImGuiHandle, new(125f.Scale(), 125f.Scale()));
                        });
                    }

                    ImGui.Spacing();
                    ImGui.Separator();

                    if (ImGui.Selectable("BiS", OpenWindow == OpenWindow.Bis))
                    {
                        OpenWindow = OpenWindow.Bis;
                    }
                    if (ImGui.Selectable("Settings", OpenWindow == OpenWindow.Settings))
                    {
                        OpenWindow = OpenWindow.Settings;
                    }

                    ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 25f);
                    ImGuiEx.LineCentered("###Donate", () => { if (ImGuiComponents.IconButtonWithText(Dalamud.Interface.FontAwesomeIcon.Coffee, $" Buy me a ko-fi? ♥")) { Util.OpenLink("https://ko-fi.com/yhumi"); }; ImGuiComponents.HelpMarker("Donations are so kind and appreciated so much, but if you find the plugin useful that's more than enough! ♥"); });
                }

                ImGui.PopStyleVar();
                ImGui.TableNextColumn();
                using (var rightChild = ImRaii.Child($"###BisTrackerRightSide", Vector2.Zero, false))
                {
                    switch (OpenWindow)
                    {
                        case OpenWindow.Bis:
                            BiSUI.Draw();
                            break;
                        case OpenWindow.Settings:
                            SettingsUI.Draw();
                            break;
                        case OpenWindow.None:
                            break;
                        default:
                            break;
                    };
                }
            }
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }
}

public enum OpenWindow
{
    None = 0,
    Bis = 1,
    Settings = 2
}
