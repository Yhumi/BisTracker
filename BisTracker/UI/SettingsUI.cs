using Dalamud.Interface.Components;
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
        }
    } 
}
