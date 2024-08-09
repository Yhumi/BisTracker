using BisTracker.RawInformation;
using BisTracker.RawInformation.Character;
using ECommons.DalamudServices;
using ImGuiNET;
using ECommons.ImGuiMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BisTracker.BiS.Models;
using Dalamud.Interface.Utility.Raii;
using ECommons.ExcelServices;

namespace BisTracker.BiS
{
    internal static class BiSUI
    {
        internal static uint SelectedJob = 0;
        internal static string SelectedJobPreview = string.Empty;

        internal static string SelectedSavedSet = string.Empty;
        internal static string StoredBisSearch = string.Empty;

        internal static string SetNameToSave = string.Empty;

        private static string Search = string.Empty;
        private static string BisLink = string.Empty;
        private static Uri? BisLinkUri = null!;

        private static BisSheetType SheetType = BisSheetType.None;
        private static XivGearAppResponse? XivGearAppResponse;
        private static string XivGearAppSetSearch = string.Empty;
        private static string SelectedXivGearAppSet = string.Empty;
        private static XivGearApp_SetItems? XivGearAppChosenBis = null;

        private static string[] ValidHosts = ["xivgear.app", "www.xivgear.app"];

        internal static void Draw()
        {
            ImGui.TextWrapped($"This page allows you to set your BIS for the jobs in the game.\n\n" +
                $"Currently accepted links:\n" + 
                "- xivgearapp");
            
            ImGui.Separator();
            
            DrawBisUI();
        }

        private static async void DrawBisUI()
        {
            if (ImGui.Button($"Current job: {CharacterInfo.JobID}"))
            {
                ResetBis();
                ResetInputs();
                SelectedJob = CharacterInfo.JobIDUint;
                LoadBisFromConfig();
            }

            ImGui.TextWrapped("Job");
            SelectedJobPreview = SelectedJob != 0 ? LuminaSheets.ClassJobSheet[SelectedJob].Name.RawString : string.Empty;
            if (ImGui.BeginCombo("###BisJobSelection", SelectedJobPreview))
            {
                ImGui.Text("Search");
                ImGui.SameLine();
                ImGui.InputText("###BisJobSearch", ref Search, 100);

                if (ImGui.Selectable("", SelectedJob == 0))
                {
                    if (0 != SelectedJob) { ResetBis(); }
                    SelectedJob = 0;
                    SelectedJobPreview = string.Empty;
                }

                foreach (var job in LuminaSheets.ClassJobSheet.Values.Where(x => x.Name.RawString.Contains(Search, StringComparison.CurrentCultureIgnoreCase) || x.Abbreviation.RawString.Contains(Search, StringComparison.CurrentCultureIgnoreCase))) 
                {
                    bool selected = ImGui.Selectable($"{job.Name.RawString}", job.RowId == SelectedJob);

                    if (selected)
                    {
                        if (job.RowId != SelectedJob) 
                        { 
                            ResetBis();
                            ResetInputs();
                        }
                        SelectedJob = job.RowId;
                        SelectedJobPreview = SelectedJob != 0 ? LuminaSheets.ClassJobSheet[SelectedJob].Name.RawString : string.Empty;
                        LoadBisFromConfig();
                    }
                }

                ImGui.EndCombo();
            }

            if (SelectedJob != 0)
            {
                if (P?.Config?.SavedBis != null)
                {
                    ImGui.Separator();
                    ImGui.TextWrapped("Stored Sets");
                    if (ImGui.BeginCombo("###StoredBisSets", SelectedSavedSet))
                    {
                        ImGui.Text("Search");
                        ImGui.SameLine();
                        ImGui.InputText("###StoredBisSetSearch", ref StoredBisSearch, 100);

                        if (ImGui.Selectable("", SelectedSavedSet == string.Empty))
                        {
                            if (string.Empty != SelectedSavedSet) { ResetBis(); }
                            SelectedSavedSet = string.Empty;
                        }

                        foreach (var savedSet in P.Config.SavedBis.Where(x => x.Job == SelectedJob))
                        {
                            bool selected = ImGui.Selectable($"{savedSet.Name}", savedSet.Name == SelectedSavedSet);

                            if (selected)
                            {
                                SelectedSavedSet = savedSet.Name;
                                ResetBis();
                                LoadBisFromConfig(savedSet.Name);
                            }
                        }

                        ImGui.EndCombo();
                    }
                }

                ImGui.Separator();
                ImGui.TextWrapped("Bis Link");
                ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X / 2);
                ImGui.InputText("###BisLinkInput", ref BisLink, 300);
                ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2 + 10f.Scale());
                if(ImGui.Button($"Import Bis"))
                {
                    ResetBis();
                    SelectedSavedSet = string.Empty;

                    Uri.TryCreate(BisLink, UriKind.Absolute, out BisLinkUri);
                    if (BisLinkUri == null || !ValidHosts.Contains(BisLinkUri.Host)) { ImGui.TextWrapped($"Invalid URI."); return; }

                    FetchBisFromHost();
                }

                ImGui.Separator();

                if (SheetType == BisSheetType.XIVGearApp)
                {
                    if (XivGearAppResponse == null && XivGearAppChosenBis == null) { ImGui.TextWrapped($"Fetching bis from: {BisLinkUri.Host}..."); return; }
                    if (XivGearAppResponse != null && XivGearAppResponse.Error) { ImGui.TextWrapped($"An error occurred fetching from: {BisLinkUri?.AbsoluteUri ?? ""}."); return; }

                    if (XivGearAppResponse != null && XivGearAppResponse.Sets != null)
                    {
                        DrawXivGearAppSets();
                        return;
                    }

                    if (XivGearAppChosenBis == null && XivGearAppResponse != null && XivGearAppResponse.Items != null)
                    {
                        XivGearAppChosenBis = XivGearAppResponse.Items;
                        DrawXivGearAppItems();
                        return;
                    }
                    
                    if (XivGearAppChosenBis != null)
                    {
                        DrawXivGearAppItems();
                        return;
                    }
                }
            }
        }

        private static void DrawXivGearAppSets()
        {
            if (XivGearAppResponse?.Sets == null) { return; }

            if (ImGui.BeginCombo("###BisXivGearAppSetSelection", SelectedXivGearAppSet))
            {
                ImGui.Text("Search");
                ImGui.SameLine();
                ImGui.InputText("###BisXivGearAppSetSearch", ref XivGearAppSetSearch, 100);

                if (ImGui.Selectable("", SelectedXivGearAppSet == string.Empty))
                {
                    SelectedXivGearAppSet = string.Empty;
                    XivGearAppChosenBis = null;
                }

                foreach (var set in XivGearAppResponse.Sets.Where(x => x.Items != null && x.Items.Weapon != null).Where(x => x.Name != null && x.Name.Contains(XivGearAppSetSearch, StringComparison.CurrentCultureIgnoreCase)))
                {
                    bool selected = ImGui.Selectable($"{set.Name}", set.Name == SelectedXivGearAppSet);

                    if (selected)
                    {
                        SelectedXivGearAppSet = set.Name;
                        XivGearAppChosenBis = set.Items;
                    }
                }

                ImGui.EndCombo();
            }            

            if (XivGearAppChosenBis != null)
            {
                DrawXivGearAppItems();
            }
        }

        private static void DrawXivGearAppItems()
        {
            if (XivGearAppChosenBis == null) { return; }

            if (SelectedSavedSet == string.Empty)
            {
                ImGui.InputText("###SaveBisSet", ref SetNameToSave, 100);
                ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2 + 10f.Scale());
                if (ImGui.Button($"Save Selection"))
                {
                    SaveBisSelection();
                }
            }
            
            ImGui.Text("Weapon");
            if (XivGearAppChosenBis?.Weapon != null)
            {
                ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Weapon.Id]?.Name ?? "");
                if (XivGearAppChosenBis.Weapon.Materia != null)
                {
                    foreach (var materia in XivGearAppChosenBis.Weapon.Materia)
                    {
                        if (materia.Id > -1)
                        {
                            ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                        }
                    }
                }
            }

            using (var table = ImRaii.Table($"XivGearAppBisTable", 2, ImGuiTableFlags.Resizable))
            {
                //Head | Ears
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Text("Head");
                if (XivGearAppChosenBis?.Head != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Head.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.Head.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.Head.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();
                ImGui.Text("Ears");
                if (XivGearAppChosenBis?.Ears != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Ears.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.Ears.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.Ears.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                //Body | Neck
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Text("Body");
                if (XivGearAppChosenBis?.Body != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Body.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.Body.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.Body.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();
                ImGui.Text("Neck");
                if (XivGearAppChosenBis?.Neck != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Neck.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.Neck.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.Neck.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                //Hands | Wrists
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Text("Hands");
                if (XivGearAppChosenBis?.Hand != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Hand.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.Hand.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.Hand.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();
                ImGui.Text("Wrists");
                if (XivGearAppChosenBis?.Wrist != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Wrist.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.Wrist.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.Wrist.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                //Legs | RightRing
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Text("Legs");
                if (XivGearAppChosenBis?.Legs != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Legs.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.Legs.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.Legs.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();
                ImGui.Text("Right Ring");
                if (XivGearAppChosenBis?.RingRight != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.RingRight.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.RingRight.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.RingRight.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                //Feet | LeftRing
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Text("Feet");
                if (XivGearAppChosenBis?.Feet != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.Feet.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.Feet.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.Feet.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }

                ImGui.TableNextColumn();
                ImGui.Text("Left Ring");
                if (XivGearAppChosenBis?.RingLeft != null)
                {
                    ImGui.Text(LuminaSheets.ItemSheet?[(uint)XivGearAppChosenBis.RingLeft.Id]?.Name ?? "");
                    if (XivGearAppChosenBis.RingLeft.Materia != null)
                    {
                        foreach (var materia in XivGearAppChosenBis.RingLeft.Materia)
                        {
                            if (materia.Id > -1)
                            {
                                ImGui.Text(LuminaSheets.ItemSheet?[(uint)materia.Id]?.Name ?? "");
                            }
                        }
                    }
                }
            }
        }

        private static void ResetBis()
        {
            SheetType = BisSheetType.None;
            XivGearAppResponse = null;
            XivGearAppChosenBis = null;
        }

        private static void ResetInputs()
        {
            BisLink = string.Empty;
            BisLinkUri = null;
            XivGearAppSetSearch = string.Empty;
            SelectedXivGearAppSet = string.Empty;
        }

        private static void FetchBisFromHost()
        {
            if (BisLinkUri == null) { return; }
            switch(BisLinkUri.Host)
            {
                case "xivgear.app":
                case "www.xivgear.app":
                    SheetType = BisSheetType.XIVGearApp;
                    FetchBisFromXivgearApp();
                    break;
                default:
                    break;
            }
        }

        private static async void FetchBisFromXivgearApp()
        {
            if (BisLinkUri == null) return;
            XivGearAppResponse = await BisSheetReader.XivGearApp(BisLinkUri);
            if (XivGearAppResponse == null) XivGearAppResponse = new(true);

            Svc.Log.Debug($"XivGearApp Reponse (Error? {XivGearAppResponse?.Error.ToString() ?? "NULL"}): {XivGearAppResponse?.Name ?? "NULL"}");
        }
    
        private static void SaveBisSelection()
        {
            JobBis jobBis = new JobBis()
            {
                Job = SelectedJob,
                SheetType = SheetType,
                Link = BisLink,
                SelectedXivGearAppSet = SelectedXivGearAppSet,
                XivGearAppSetItems = XivGearAppResponse.Items,
                Name = SetNameToSave
            };

            if (SelectedXivGearAppSet != string.Empty && XivGearAppResponse.Sets != null)
            {
                jobBis.XivGearAppSetItems = XivGearAppResponse.Sets.Where(x => x.Name == SelectedXivGearAppSet).FirstOrDefault()?.Items ?? null;
            }

            P.Config.SaveJobBis(jobBis);
        }
    
        private static void LoadBisFromConfig(string? setName = null)
        {
            var jobBis = setName != null ? P.Config.SavedBis?.Where(x => x.Name == setName).SingleOrDefault() : P.Config.SavedBis?.Where(x => x.Job == SelectedJob).FirstOrDefault() ?? null;
            if (jobBis == null) return;
            if (setName == null) { SelectedSavedSet = jobBis.Name; }

            Svc.Log.Debug($"Saved bis found for {SelectedJobPreview}: {jobBis.Link}");

            BisLink = jobBis.Link;
            
            Uri.TryCreate(BisLink, UriKind.Absolute, out BisLinkUri);
            if (BisLinkUri == null || !ValidHosts.Contains(BisLinkUri.Host)) { ImGui.TextWrapped($"Invalid URI."); return; }

            SheetType = jobBis.SheetType ?? BisSheetType.None;

            if (SheetType == BisSheetType.XIVGearApp)
            {
                SelectedXivGearAppSet = jobBis.SelectedXivGearAppSet ?? string.Empty;
                XivGearAppChosenBis = jobBis.XivGearAppSetItems;
            }
        }
    }
}

public enum BisSheetType
{
    None = 0,
    XIVGearApp = 1
}
