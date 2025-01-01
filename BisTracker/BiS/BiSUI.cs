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
using System.Numerics;
using OtterGui;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ECommons.Automation;
using BisTracker.Melding;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule.Delegates;
using System.Xml.Linq;
using Lumina.Excel.Sheets;

namespace BisTracker.BiS
{
    internal static class BiSUI
    {
        internal static uint SelectedJob = 0;
        internal static string SelectedJobPreview = string.Empty;

        internal static string SelectedSavedSet = string.Empty;
        internal static string StoredBisSearch = string.Empty;

        internal static string SetNameToSave = string.Empty;

        internal static JobBis? TempBis = null;

        private static string Search = string.Empty;
        private static string BisLink = string.Empty;
        private static Uri? BisLinkUri = null!;

        private static BisSheetType SheetType = BisSheetType.None;

        //Loading from XivGear.App
        private static XivGearAppResponse? XivGearAppResponse;
        private static string XivGearAppSetSearch = string.Empty;
        private static string SelectedXivGearAppSet = string.Empty;
        private static XivGearApp_SetItems? XivGearAppChosenBis = null;
        private static int? XivGearAppChosenFood = null;

        //Loading from Etro
        private static EtroResponse? EtroResponse;

        //Loading from saved bis
        private static JobBis? SavedBis = null;

        //Found slots 
        private static List<CharacterEquippedGearSlotIndex> FoundItems = new();
        private static List<CharacterEquippedGearSlotIndex> EquippedItems = new();

        private static readonly string[] ExcludedJobs = ["CNJ", "ADV", "ARC", "GLA", "THM", "PGL", "MRD", "LNC", "ACN", "ROG"];
        private static string[] ValidHosts = ["xivgear.app", "www.xivgear.app", "etro.gg", "www.etro.gg"];

        internal static void Draw()
        {
            ImGui.TextWrapped($"This page allows you to set your BIS for the jobs in the game.\n\n" +
                $"Currently accepted links:\n" + 
                "- xivgear.app\n" +
                "- etro.gg");
            
            ImGui.Separator();
            
            DrawBisUI();
        }

        private static unsafe async void DrawBisUI()
        {
            if (ImGui.Button($"Select Current job ({CharacterInfo.JobID})"))
            {
                ResetBis();
                ResetInputs();
                SelectedJob = CharacterInfo.JobIDUint;
                LoadBisFromConfig();
            }
            ImGui.SameLine();
            if (ImGuiEx.ButtonCond("Equip Items", () => IsBisSelected() && EquipGear.Status == EquipGear.EquipGearStatus.Idle))
            {
                var jobBis = GetJobBis();
                if (jobBis == null) return;

                EquipGear.Tasks.Add((() => EquipGear.EquipGearset((Job)SelectedJob), TimeSpan.FromMilliseconds(200)));
                foreach (var item in jobBis.BisItems)
                {
                    EquipGear.Tasks.Add((() => EquipGear.EquipGearPiece(item.Id), TimeSpan.FromMilliseconds(200)));
                }
            }

            if (UIState.Instance()->IsUnlockLinkUnlocked(12))
            {
                ImGui.SameLine();
                if (ImGuiEx.ButtonCond("Open Materia Melder", () => IsBisSelected() && SelectedJob == CharacterInfo.JobIDUint))
                {
                    if (IsBisSelected())
                        PopulateMeldBis();

                    P.TM.Enqueue(() => MeldingAddonUtils.OpenMateriaMelder());
                    P.TM.Enqueue(() => MeldingAddonUtils.SelectEquippedItemsDropdown());
                }
            }  

            if (ImGui.CollapsingHeader("BiS Selection", ImGuiTreeNodeFlags.Selected))
            {
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
                        .Where(x => !ExcludedJobs.Contains(x.Abbreviation.ToString()))
                        .Where(x => x.Name.ToString().Contains(Search, StringComparison.CurrentCultureIgnoreCase) || x.Abbreviation.ToString().Contains(Search, StringComparison.CurrentCultureIgnoreCase))
                        .OrderBy(x => x.Name.ToString()))
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
                        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X / 2);
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

                        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2 + 10f.Scale());
                        if (ImGuiUtil.DrawDisabledButton($"Delete Set", default(Vector2), DeleteButtonTooltip(), !ImGui.GetIO().KeyCtrl, false))
                        {
                            ResetBis(); //Deselect the set.
                            DeleteBisByName(SelectedSavedSet);
                            SelectedSavedSet = string.Empty;
                        }
                    }

                    ImGui.Separator();
                    ImGui.TextWrapped("Bis Link");
                    ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X / 2);
                    ImGui.InputText("###BisLinkInput", ref BisLink, 300);
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2 + 10f.Scale());
                    if (ImGui.Button($"Import Bis"))
                    {
                        ResetBis();
                        SelectedSavedSet = string.Empty;

                        Uri.TryCreate(BisLink, UriKind.Absolute, out BisLinkUri);
                        if (BisLinkUri == null || !ValidHosts.Contains(BisLinkUri.Host)) { ImGui.TextWrapped($"Invalid URI."); return; }

                        FetchBisFromHost();
                    }

                    ImGui.Separator();
                    if (SheetType != BisSheetType.None && SheetType != BisSheetType.Saved)
                    {
                        ImGui.InputText("###SaveBisSet", ref SetNameToSave, 100);
                        ImGui.SameLine(ImGui.GetContentRegionAvail().X / 2 + 10f.Scale());
                        if (ImGui.Button($"Save Selection"))
                        {
                            SaveBisSelection();
                        }
                    }
                }
            }

            if (SheetType != BisSheetType.None && ImGui.CollapsingHeader("BiS Items"))
            {
                switch (SheetType)
                {
                    case BisSheetType.Saved:
                        DrawSavedBis();
                        break;
                    case BisSheetType.XIVGearApp:
                        DrawXivGearAppBis();
                        break;
                    case BisSheetType.Etro:
                        DrawEtroBis();
                        break;
                    case BisSheetType.None:
                    default:
                        break;
                }
            }

            if (SheetType != BisSheetType.None && ImGui.CollapsingHeader("BiS Consumables"))
            {
                switch (SheetType)
                {
                    case BisSheetType.Saved:
                        DrawSavedBisConsumables();
                        break;
                    case BisSheetType.XIVGearApp:
                        DrawXivGearAppBisConsumables();
                        break;
                    case BisSheetType.Etro:
                        DrawEtroBisConsumables();
                        break;
                    case BisSheetType.None:
                    default:
                        break;
                }
            }

            if (SheetType != BisSheetType.None && ImGui.CollapsingHeader("BiS Stats"))
            {
                List<JobBis_Parameter>? setParameters = null;
                switch (SheetType)
                {
                    case BisSheetType.Saved:
                        setParameters = GetSavedSetStatistics();
                        break;
                    case BisSheetType.XIVGearApp:
                        setParameters = GetXivGearAppSetStatistics();
                        break;
                    case BisSheetType.Etro:
                        setParameters = GetEtroSetStatistics();
                        break;
                    case BisSheetType.None:
                    default:
                        return;
                }

                if (setParameters == null) return;
                setParameters = setParameters.Where(x => x.Param != 0).ToList();



                ImGuiEx.LineCentered("###BisStats", () => ImGuiEx.TextUnderlined("Set Statistics"));
                using (var table = ImRaii.Table($"BisParamTable", setParameters.Count, ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.Borders))
                {
                    var levelData = ConstantData.LevelStats[100];

                    ImGui.TableNextRow();
                    foreach (var paramName in setParameters.Select(x => x.Param))
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(LuminaSheets.BaseParamSheet[(uint)paramName].Name.ExtractText());
                    }

                    ImGui.TableNextRow();
                    foreach (var paramValue in setParameters.Select(x => x.Value))
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text(paramValue.ToString());
                    }
                }

                //ImGuiEx.LineCentered("###TomeStats", () => ImGuiEx.TextUnderlined("Tomes"));
                //ImGuiEx.LineCentered("###CenterTable", () =>
                //{
                //    using (var table = ImRaii.Table($"BisTomeStats", 3, ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.Borders, new Vector2((ImGui.GetContentRegionAvail() / 2).X, 0)))
                //    {
                //        ImGui.TableSetupColumn("Total Tomes");
                //        ImGui.TableSetupColumn("Remaining Tomes");
                //        ImGui.TableSetupColumn("Weeks Remaining");

                //        ImGui.TableHeadersRow();
                //        ImGui.TableNextRow();

                //        for (var i = 0; i < 3; i++)
                //        {
                //            ImGui.TableNextColumn();
                //            ImGui.Text("0");
                //        }
                //    }
                //});
                

            }
        }

        private static string DeleteButtonTooltip()
            => "Delete Current Selection. Hold Control while clicking.";

        private static void DrawXivGearAppBis()
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

        private static void DrawEtroBis()
        {
            if (EtroResponse == null) { ImGui.TextWrapped($"Fetching bis from: {BisLinkUri.Host}..."); return; }
            if (EtroResponse != null && EtroResponse.Error) { ImGui.TextWrapped($"An error occurred fetching from: {BisLinkUri?.AbsoluteUri ?? ""}."); return; }

            DrawEtroItems();
        }

        private static void DrawSavedBis()
        {
            if (SavedBis == null) return;
            DrawBisItems(SavedBis);
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
                    XivGearAppChosenFood = null;
                }

                foreach (var set in XivGearAppResponse.Sets.Where(x => x.Items != null && x.Items.Weapon != null).Where(x => x.Name != null && x.Name.Contains(XivGearAppSetSearch, StringComparison.CurrentCultureIgnoreCase)))
                {
                    bool selected = ImGui.Selectable($"{set.Name}", set.Name == SelectedXivGearAppSet);

                    if (selected)
                    {
                        Svc.Log.Debug($"Selected Set: {set.Name}");
                        TempBis = null;
                        SelectedXivGearAppSet = set.Name;
                        XivGearAppChosenBis = set.Items;
                        XivGearAppChosenFood = set.Food;

                        var bis = GetJobBis();
                        if (bis == null) return;

                        TempBis = bis;

                        CheckForItemsEquipped(bis);
                        CheckForItemsInArmouryChest(bis);
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
            if (TempBis == null) { return; }
            DrawBisItems(TempBis);
        }

        private static void DrawEtroItems()
        {
            if (TempBis == null) { return; }
            DrawBisItems(TempBis);
        }

        private static void DrawBisItems(JobBis jobBis)
        {
            if (jobBis == null || jobBis.BisItems == null) return;

            using (var table = ImRaii.Table($"XivGearAppBisTable", 2, ImGuiTableFlags.Resizable))
            {
                //MainHand | OffHand
                ImGui.TableNextRow();

                DrawItem("Weapon", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.MainHand).FirstOrDefault(), CharacterEquippedGearSlotIndex.MainHand);
                DrawItem("Off Hand", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.OffHand).FirstOrDefault(), CharacterEquippedGearSlotIndex.OffHand);

                //Head | Ears
                ImGui.TableNextRow();

                DrawItem("Head", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.Head).FirstOrDefault(), CharacterEquippedGearSlotIndex.Head);
                DrawItem("Ears", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.Ears).FirstOrDefault(), CharacterEquippedGearSlotIndex.Ears);

                //Body | Neck
                ImGui.TableNextRow();

                DrawItem("Body", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.Body).FirstOrDefault(), CharacterEquippedGearSlotIndex.Body);
                DrawItem("Neck", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.Neck).FirstOrDefault(), CharacterEquippedGearSlotIndex.Neck);

                //Hands | Wrists
                ImGui.TableNextRow();

                DrawItem("Hands", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.Gloves).FirstOrDefault(), CharacterEquippedGearSlotIndex.Gloves);
                DrawItem("Wrist", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.Wrists).FirstOrDefault(), CharacterEquippedGearSlotIndex.Wrists);

                //Legs | RightRing
                ImGui.TableNextRow();

                DrawItem("Legs", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.Legs).FirstOrDefault(), CharacterEquippedGearSlotIndex.Legs);
                DrawItem("Right Ring", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.RightRing).FirstOrDefault(), CharacterEquippedGearSlotIndex.RightRing);

                //Feet | LeftRing
                ImGui.TableNextRow();

                DrawItem("Feet", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.Feet).FirstOrDefault(), CharacterEquippedGearSlotIndex.Feet);
                DrawItem("Left Ring", jobBis.BisItems.Where(x => x.GearSlot == CharacterEquippedGearSlotIndex.LeftRing).FirstOrDefault(), CharacterEquippedGearSlotIndex.LeftRing);
            }
        }

        private static void DrawItem(string itemSlot, JobBis_Item? gearAppItem, CharacterEquippedGearSlotIndex gearSlot)
        {
            ImGui.TableNextColumn();

            if (gearAppItem == null || gearAppItem.Id <= 0) return;
            Item luminaItem = LuminaSheets.ItemSheet[(uint)gearAppItem.Id];

            if (EquippedItems.Contains(gearSlot))
                itemSlot = $"{itemSlot} (Equipped)";
            if (FoundItems.Contains(gearSlot))
                itemSlot = $"{itemSlot} (In Armoury Chest)";

            ImGui.Text(itemSlot);
            using (var table = ImRaii.Table($"#XivItem-{itemSlot}", 2, ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawItemIcon(luminaItem);
                ImGui.TableNextColumn();

                ImGui.Text(luminaItem.Name.ToString() ?? "");
                if (gearAppItem.Materia != null)
                {
                    foreach (var materia in gearAppItem.Materia)
                    {
                        if (materia.Id > 0)
                        {
                            ImGui.Text($"\t{materia.GetMateriaLabel()}");
                        }
                    }
                }
            }
        }

        private static void DrawSavedBisConsumables()
        {
            if (SavedBis == null) return;
            DrawBisConsumables(SavedBis);
        }

        private static void DrawXivGearAppBisConsumables()
        {
            if (XivGearAppChosenBis == null) { return; }

            //var bis = GetJobBis();
            //if (bis == null) return;

            DrawBisConsumables(TempBis);
        }

        private static void DrawEtroBisConsumables()
        {
            if (EtroResponse == null) { return; }

            DrawBisConsumables(TempBis);
        }

        public static void DrawBisConsumables(JobBis bis)
        {
            if (bis == null) return;
            if (bis.Food == null && bis.Medicine == null)
            {
                ImGui.Text("No consumables used.");
                return;
            }

            using (var table = ImRaii.Table($"BisConsumableTable", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                using (var innerTable = ImRaii.Table($"#BisConsumable-Food", 2, ImGuiTableFlags.SizingFixedFit))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                        ImGui.Text("No food used.");
                }

                ImGui.TableNextColumn();
                ImGui.Text("No medicine used.");
            }
        }

        private static List<JobBis_Parameter>? GetSavedSetStatistics()
        {
            if (SavedBis == null || SavedBis.SetParameters == null) return null;
            return SavedBis.SetParameters;
        }

        private static List<JobBis_Parameter>? GetEtroSetStatistics()
        {
            if (EtroResponse == null) { return null; }

            JobBis? jobBis = GetJobBis();
            if (jobBis == null) return null;

            return jobBis.SetParameters;
        }

        private static List<JobBis_Parameter>? GetXivGearAppSetStatistics()
        {
            var jobBis = GetJobBis();
            if (jobBis == null) return null;

            return jobBis.SetParameters;
        }

        private static void GetSavedSetTomes()
        {

        }

        private static void DrawItemIcon(Item icon)
        {
            P.Icons.TryLoadIcon(icon.Icon, out IDalamudTextureWrap? wrap);
            if (wrap != null) ImGuiUtil.HoverIcon(wrap, new Vector2(64f, 64f));
        }

        public static void ResetBis()
        {
            SheetType = BisSheetType.None;
            XivGearAppResponse = null;
            XivGearAppChosenBis = null;
            SavedBis = null;
            TempBis = null;
            EtroResponse = null;
            EquippedItems.Clear();
            FoundItems.Clear();
        }

        public static void ResetInputs()
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
                case "etro.gg":
                case "www.etro.gg":
                    SheetType = BisSheetType.Etro;
                    FetchBisFromEtro();
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
            XivGearAppChosenFood = XivGearAppResponse.Food;

            Svc.Log.Debug($"XivGearApp Reponse (Error? {XivGearAppResponse?.Error.ToString() ?? "NULL"}): {XivGearAppResponse?.Name ?? "NULL"}");

            if (XivGearAppResponse!.Items != null)
            {
                var jobBis = GetJobBis();
                TempBis = jobBis;

                CheckForItemsEquipped(jobBis);
                CheckForItemsInArmouryChest(jobBis);
            }
        }

        private static async void FetchBisFromEtro()
        {
            if (BisLinkUri == null) return;
            EtroResponse = await BisSheetReader.Etro(BisLinkUri);
            if (EtroResponse == null) EtroResponse = new(true);

            Svc.Log.Debug($"Etro Reponse (Error? {EtroResponse?.Error.ToString() ?? "NULL"}): {EtroResponse?.Name ?? "NULL"}");
            TempBis = GetJobBis();

            EtroItemCheck();
        }

        public static void UpdateItemCheck()
        {
            EquippedItems.Clear();
            FoundItems.Clear();

            switch(SheetType)
            {
                case BisSheetType.Saved:
                    if (SavedBis == null) return;
                    CheckForItemsEquipped(SavedBis);
                    CheckForItemsInArmouryChest(SavedBis);
                    GetSavedSetStatistics();
                    break;

                case BisSheetType.Etro:
                    EtroItemCheck();
                    GetEtroSetStatistics();
                    break;

                case BisSheetType.XIVGearApp:
                    XIVGearAppItemCheck();
                    GetXivGearAppSetStatistics();
                    break;

                case BisSheetType.None:
                default:
                    break;
            }
        }

        private static void EtroItemCheck()
        {
            if (TempBis == null) return;

            CheckForItemsEquipped(TempBis);
            CheckForItemsInArmouryChest(TempBis);
        }

        private static void XIVGearAppItemCheck()
        {
            if (TempBis == null) return;

            CheckForItemsEquipped(TempBis);
            CheckForItemsInArmouryChest(TempBis);
        }

        private static unsafe void CheckForItemsEquipped(JobBis bis)
        {
            if (bis.BisItems == null) return;
            foreach (var item in bis.BisItems)
            {
                if (item.Id == 0) { continue; }

                //Svc.Log.Debug($"Checking for equipped item: {item.Id}");

                var equippedGear = CharacterInfo.EquippedGear->GetInventorySlot((int)item.GearSlot);

                if ((item.GearSlot == CharacterEquippedGearSlotIndex.RightRing && (equippedGear == null || equippedGear->ItemId != item.Id)))
                    equippedGear = CharacterInfo.EquippedGear->GetInventorySlot((int)CharacterEquippedGearSlotIndex.LeftRing);
                if ((item.GearSlot == CharacterEquippedGearSlotIndex.LeftRing && (equippedGear == null || equippedGear->ItemId != item.Id)))
                    equippedGear = CharacterInfo.EquippedGear->GetInventorySlot((int)CharacterEquippedGearSlotIndex.RightRing);

                if (equippedGear == null) { continue; }
                 
                if (equippedGear->ItemId == item.Id)
                {
                    Svc.Log.Debug($"Found equipped item: {item.Id}");
                    EquippedItems.Add(item.GearSlot);
                }
            }
        }

        private static unsafe void CheckForItemsInArmouryChest(JobBis bis)
        {
            if (bis.BisItems == null) return;
            foreach (var item in bis.BisItems.Where(x => !FoundItems.Contains(x.GearSlot)))
            {
                if (item.Id == 0) { continue; }
                if (EquippedItems.Contains(item.GearSlot)) { continue; }

                Svc.Log.Debug($"Checking for armoury chest ({item.GearSlot.ToString()}) item: {item.Id}");
                var acItem = CharacterInfo.SearchForItemInArmouryChest(item.Id, item.GearSlot);
                if (acItem != null)
                {
                    Svc.Log.Debug($"Found armoury chest ({item.GearSlot.ToString()}) item: {item.Id}");
                    FoundItems.Add(item.GearSlot);
                }
            }
        }

        private static void SaveBisSelection()
        {
            JobBis jobBis = new JobBis()
            {
                Job = SelectedJob
            };

            switch(SheetType)
            {
                case BisSheetType.XIVGearApp:
                    if (XivGearAppResponse == null) return;
                    jobBis.PopulateBisItemsFromXIVGearApp(XivGearAppResponse, SelectedXivGearAppSet);
                    break;
                case BisSheetType.Etro:
                    if (EtroResponse == null) return;
                    jobBis.PopulateBisItemsFromEtro(EtroResponse);
                    break;
                default:
                    return;
            }

            jobBis.Name = SetNameToSave;

            P.Config.SaveJobBis(jobBis);
        }

        private static string JobNameCleanup(ClassJob job)
        {
            string jobNameCapitalised = char.ToUpper(job.Name.ToString().First()) + job.Name.ToString().Substring(1).ToLower();
            return $"{jobNameCapitalised} ({job.Abbreviation.ToString()})";
        }
    
        private static void LoadBisFromConfig(string? setName = null)
        {
            var jobBis = setName != null ? new JobBis(P.Config.SavedBis?.Where(x => x.Name == setName).SingleOrDefault()) : new JobBis(P.Config.SavedBis?.Where(x => x.Job == SelectedJob).FirstOrDefault());
            if (jobBis == null || jobBis.BisItems == null) return;
            if (setName == null) { SelectedSavedSet = jobBis.Name; }

            jobBis.SetupItemStatistics();
            //jobBis.CalculateSetPrice();

            Svc.Log.Debug($"Saved bis found for {SelectedJobPreview}.");
            SheetType = BisSheetType.Saved;
            SavedBis = jobBis;

            CheckForItemsEquipped(jobBis);
            CheckForItemsInArmouryChest(jobBis);
        }

        private static void DeleteBisByName(string? setName = null)
        {
            if (setName == null) return;
            var removedSets = setName != null ? P.Config.SavedBis?.RemoveAll(x => x.Name == setName && x.Job == SelectedJob) : null;
            if (removedSets == 0) return;

            Svc.NotificationManager.AddNotification(new Dalamud.Interface.ImGuiNotification.Notification() { Content = $"Deleted set for {JobNameCleanup(LuminaSheets.ClassJobSheet[SelectedJob])} with name {setName}", Type = Dalamud.Interface.ImGuiNotification.NotificationType.Success });
            P.Config.Save();
        }

        private static bool IsBisSelected()
        {
            return EtroResponse != null || XivGearAppChosenBis != null || SavedBis != null;
        }     

        private static void PopulateMeldBis()
        {
            switch (SheetType)
            {
                case BisSheetType.Saved:
                    if (SavedBis == null) return;
                    P.MeldUI.SetBis(SavedBis);
                    break;

                case BisSheetType.Etro:
                    if (EtroResponse == null) return;
                    var etroBis = GetJobBis();
                    if (etroBis == null) return;
                    P.MeldUI.SetBis(etroBis);
                    break;

                case BisSheetType.XIVGearApp:
                    if (XivGearAppResponse == null) return;
                    var xivAppBis = GetJobBis();
                    if (xivAppBis == null) return;
                    P.MeldUI.SetBis(xivAppBis);
                    break;

                case BisSheetType.None:
                default:
                    break;
            }
        }

        private static JobBis? GetJobBis()
        {
            switch (SheetType)
            {
                case BisSheetType.Saved:
                    return SavedBis;

                case BisSheetType.Etro:
                    if (TempBis != null) return TempBis;
                    if (EtroResponse == null) return null;
                    var etroBis = new JobBis();
                    etroBis.PopulateBisItemsFromEtro(EtroResponse);
                    return etroBis;

                case BisSheetType.XIVGearApp:
                    if (TempBis != null) return TempBis;
                    if (XivGearAppResponse == null) return null;
                    var xivAppBis = new JobBis();
                    xivAppBis.PopulateBisItemsFromXIVGearApp(XivGearAppResponse, SelectedXivGearAppSet);
                    return xivAppBis;

                case BisSheetType.None:
                default:
                    return null;
            }
        }
    }
}

public enum BisSheetType
{
    None = 0,
    Saved = 1,
    XIVGearApp = 2,
    Etro = 3
}
