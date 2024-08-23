using Dalamud.Interface.Colors;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.UI
{
    internal static unsafe class SettingsUI
    {
        internal static void Draw()
        {
            ImGui.TextWrapped($"Here you can change some of the main settings for BisTracker.");

            bool ShowMateriaMeldingWindows = P.Config.ShowMateriaMeldingWindows;
            bool HighlightBisMateriaInMateriaMelder = P.Config.HighlightBisMateriaInMateriaMelder;
            bool ShowAugmentedMeldsForUnaugmentedPieces = P.Config.ShowAugmentedMeldsForUnaugmentedPieces;
            bool UseMateriaNameInsteadOfMateriaValue = P.Config.UseMateriaNameInsteadOfMateriaValue;

            int GenericThrottleTime = P.Config.GenericThrottleTime;
            int PauseTimeBetweenSteps = P.Config.PauseTimeBetweenSteps;
            int AnimationPauseTime = P.Config.AnimationPauseTime;

            int GenericThrottleTimeDefault = 750;
            int PauseTimeBetweenStepsDefault = 750;
            int AnimationPauseTimeDefault = 4500;

            ImGui.Separator();

            if (ImGui.CollapsingHeader("General Settings"))
            {
                if (ImGui.Checkbox("Show Materia Melding Menu Windows", ref ShowMateriaMeldingWindows))
                {
                    P.Config.ShowMateriaMeldingWindows = ShowMateriaMeldingWindows;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"Draw the windows/UI edits tied to the Materia Melding window in game.");

                if (ShowMateriaMeldingWindows)
                {
                    if (ImGui.Checkbox("Highlight BiS Materia for BiS Pieces", ref HighlightBisMateriaInMateriaMelder))
                    {
                        P.Config.HighlightBisMateriaInMateriaMelder = HighlightBisMateriaInMateriaMelder;
                        P.Config.Save();
                    }
                    ImGuiComponents.HelpMarker($"Color the names of materia to be melded into the selected BiS gear piece. Please note: This currently only works on the equipped tab.");
                }

                if (ShowMateriaMeldingWindows)
                {
                    if (ImGui.Checkbox("Show Augmented Melds for Unaugmented Pieces", ref ShowAugmentedMeldsForUnaugmentedPieces))
                    {
                        P.Config.ShowAugmentedMeldsForUnaugmentedPieces = ShowAugmentedMeldsForUnaugmentedPieces;
                        P.Config.Save();
                    }
                    ImGuiComponents.HelpMarker($"Show the melds for the augmented version of an unaugmented piece if the augmented version is part of the selected bis.");
                }

                if (ImGui.Checkbox("Use Materia Name instead of Materia Value", ref UseMateriaNameInsteadOfMateriaValue))
                {
                    P.Config.UseMateriaNameInsteadOfMateriaValue = UseMateriaNameInsteadOfMateriaValue;
                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"Show the materia name (Heavens' Eye Materia XII) instead of its value (Direct Hit +54).");
            }

            if (ImGui.CollapsingHeader("Automeld Settings"))
            {
                ImGuiEx.Text(ImGuiColors.DalamudRed, "Setting these values too low WILL cause the automelding to fail and other issues. Mess around at your own risk.");

                if (ImGui.Button("Reset to Defaults"))
                {
                    GenericThrottleTime = GenericThrottleTimeDefault;
                    P.Config.GenericThrottleTime = GenericThrottleTime;

                    PauseTimeBetweenSteps = PauseTimeBetweenStepsDefault;
                    P.Config.PauseTimeBetweenSteps = PauseTimeBetweenSteps;

                    AnimationPauseTime = AnimationPauseTimeDefault;
                    P.Config.AnimationPauseTime = AnimationPauseTime;

                    P.Config.Save();
                }
                ImGuiComponents.HelpMarker($"Reset these to the default values.");

                ImGui.Text("Generic Throttle Time");
                ImGuiComponents.HelpMarker("The wait time in miliseconds used for most throttling.");
                if (ImGui.DragInt("###GenericThrottleTime", ref GenericThrottleTime))
                {
                    P.Config.GenericThrottleTime = GenericThrottleTime;
                    P.Config.Save();
                }

                ImGui.Text("Pause Time Before Melding");
                ImGuiComponents.HelpMarker("The pause time between certain steps such as affixing materia. This is pretty likely to cause issues if you set it too low.");
                if (ImGui.DragInt("###PauseTimeBetweenSteps", ref PauseTimeBetweenSteps))
                {
                    P.Config.PauseTimeBetweenSteps = PauseTimeBetweenSteps;
                    P.Config.Save();
                }

                ImGui.Text("Animation Pause Time");
                ImGuiComponents.HelpMarker("How long to pause for while melding. Set this lower if you meld at the Materia Melder.");
                if (ImGui.DragInt("###AnimationPauseTime", ref AnimationPauseTime))
                {
                    P.Config.AnimationPauseTime = AnimationPauseTime;
                    P.Config.Save();
                }
            }
        }
    } 
}
