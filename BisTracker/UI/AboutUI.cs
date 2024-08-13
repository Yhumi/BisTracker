using Dalamud.Interface.Components;
using Dalamud.Utility;
using ECommons.ImGuiMethods;
using ImGuiNET;
using OtterGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.UI
{
    internal static unsafe class AboutUI
    {
        internal static void Draw()
        {
            ImGuiUtil.Center($"{P.Name} {P.GetType().Assembly.GetName().Version}");
            ImGuiUtil.Center("by Yhumi ♥");

            ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 25f);
            ImGuiEx.LineCentered("###Donate", () => { if (ImGuiComponents.IconButtonWithText(Dalamud.Interface.FontAwesomeIcon.Coffee, $" Buy me a ko-fi? ♥")) { Util.OpenLink("https://ko-fi.com/yhumi"); }; ImGuiComponents.HelpMarker("Donations are so kind and appreciated so much, but if you find the plugin useful that's more than enough! ♥"); });
        }
    }
}
