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
using Lumina.Excel.GeneratedSheets;
using System.Numerics;
using OtterGui;
using Dalamud.Interface.Textures.TextureWraps;

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

        private static readonly string[] ExcludedJobs = ["CNJ", "ADV", "ARC", "GLA", "THM", "PGL", "MRD", "LNC", "ACN", "ROG"];
        private static string[] ValidHosts = ["xivgear.app", "www.xivgear.app"];

        internal static void Draw()
        {
            ImGui.TextWrapped($"This page allows you to set your BIS for the jobs in the game.\n\n" +
                $"Currently accepted links:\n" + 
                "- xivgear.app");
            
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
            SelectedJobPreview = SelectedJob != 0 ? JobNameCleanup(LuminaSheets.ClassJobSheet[SelectedJob]) : string.Empty;
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

                foreach (var job in LuminaSheets.ClassJobSheet.Values
                    .Where(x => !ExcludedJobs.Contains(x.Abbreviation.RawString))
                    .Where(x => x.Name.RawString.Contains(Search, StringComparison.CurrentCultureIgnoreCase) || x.Abbreviation.RawString.Contains(Search, StringComparison.CurrentCultureIgnoreCase))
                    .OrderBy(x => x.Name.RawString))
                {
                    
                    bool selected = ImGui.Selectable(JobNameCleanup(job), job.RowId == SelectedJob);

                    if (selected)
                    {
                        if (job.RowId != SelectedJob) 
                        { 
                            ResetBis();
                            ResetInputs();
                        }
                        SelectedJob = job.RowId;
                        SelectedJobPreview = SelectedJob != 0 ? JobNameCleanup(LuminaSheets.ClassJobSheet[SelectedJob]) : string.Empty;
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

            DrawItem("Weapon", XivGearAppChosenBis.Weapon);

            using (var table = ImRaii.Table($"XivGearAppBisTable", 2, ImGuiTableFlags.Resizable))
            {
                //Head | Ears
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawItem("Head", XivGearAppChosenBis.Head);
                ImGui.TableNextColumn();
                DrawItem("Ears", XivGearAppChosenBis.Ears);

                //Body | Neck
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawItem("Body", XivGearAppChosenBis.Body);
                ImGui.TableNextColumn();
                DrawItem("Neck", XivGearAppChosenBis.Neck);

                //Hands | Wrists
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawItem("Hands", XivGearAppChosenBis.Hand);
                ImGui.TableNextColumn();
                DrawItem("Wrist", XivGearAppChosenBis.Wrist);

                //Legs | RightRing
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawItem("Legs", XivGearAppChosenBis.Legs);
                ImGui.TableNextColumn();
                DrawItem("Right Ring", XivGearAppChosenBis.RingRight);

                //Feet | LeftRing
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawItem("Feet", XivGearAppChosenBis.Feet);
                ImGui.TableNextColumn();
                DrawItem("Left Ring", XivGearAppChosenBis.RingLeft);
            }
        }

        private static void DrawItem(string itemSlot, XivGearApp_Item? gearAppItem)
        {
            if (gearAppItem == null) return;
            Item? luminaItem = LuminaSheets.ItemSheet?[(uint)gearAppItem.Id];
            if (luminaItem == null) return;

            ImGui.Text(itemSlot);
            using (var table = ImRaii.Table($"#XivItem-{itemSlot}", 2, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawItemIcon(luminaItem);
                ImGui.TableNextColumn();

                ImGui.Text(luminaItem.Name ?? "");
                if (gearAppItem.Materia != null)
                {
                    foreach (var materia in gearAppItem.Materia)
                    {
                        if (materia.Id > -1)
                        {
                            Item? materiaItem = LuminaSheets.ItemSheet?[(uint)materia.Id];
                            ImGui.Text($"\t{materiaItem?.Name}");
                        }
                    }
                }
            } 
        }

        private static void DrawItemIcon(Item icon)
        {
            P.Icons.TryLoadIcon(icon.Icon, out IDalamudTextureWrap? wrap);
            if (wrap != null) ImGuiUtil.HoverIcon(wrap, new Vector2(64f, 64f));
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
            StoredBisSearch = string.Empty;
            SelectedSavedSet = string.Empty;
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

        private static string JobNameCleanup(ClassJob job)
        {
            string jobNameCapitalised = char.ToUpper(job.Name.RawString.First()) + job.Name.RawString.Substring(1).ToLower();
            return $"{jobNameCapitalised} ({job.Abbreviation.RawString})";
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
