using BisTracker.BiS.Models;
using BisTracker.RawInformation;
using BisTracker.RawInformation.Character;
using BisTracker.Readers;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.Automation;
using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.Melding
{
    internal static unsafe class AutoMeld
    {
        internal static int CurrentWorkingPieceIndex = 0;
        internal static uint CurrentWorkingPieceId = 0;

        internal static uint SelectedWorkingJob = 0;
        internal static string SelectedWorkingBis = string.Empty;

        internal static bool AutoUnmelding = false;
        internal static bool AutoMelding = false;

        internal static bool PerformingAction = false;
        internal static bool Throttled = false;
        internal static bool Initialised = false;

        internal static bool ItemSelected = false;
        internal static bool MateriaSelected = false;
        internal static bool AffixingMateria = false;

        internal static bool ItemRightClicked = false;
        internal static bool RetrieveDialogOpened = false;
        internal static bool RetrievingMateria = false;

        internal static bool Aborting = false;

        internal static bool Errors = false;

        public static void Init()
        {
            AutoUnmelding = false;
            AutoMelding = false;
            Initialised = true;
        }

        public static void Tick()
        {
            if (Aborting) return;

            if (AutoUnmelding && !PerformingAction)
            {
                Svc.Log.Debug("Checking existing materia.");
                PerformingAction = true;
                var equippedItem = CharacterInfo.GetEquippedItem((int)CurrentWorkingPieceId);
                var bis = P.Config.SavedBis?.Where(x => x.Job == SelectedWorkingJob && x.Name == SelectedWorkingBis).FirstOrDefault() ?? null;
                var bisMateriaCount = bis.BisItems?.FirstOrDefault(x => x.Id == (int)CurrentWorkingPieceId)?.Materia?.Count;
                var materiaAffixed = equippedItem->Materia.ToArray().Where(x => x != 0);

                Svc.Log.Debug($"Affixed: {materiaAffixed.Count()}. | Bis count: {bisMateriaCount}");

                if (materiaAffixed.Count() > bisMateriaCount || !CheckAllPreviousMateriaMatch(equippedItem, bis.BisItems?.FirstOrDefault(x => x.Id == (int)CurrentWorkingPieceId)?.Materia))
                {
                    if (ItemRightClicked && RetrieveDialogOpened && RetrievingMateria)
                    {
                        Svc.Log.Debug("Finished unmelding");
                        ItemRightClicked = false;
                        RetrieveDialogOpened = false;
                        RetrievingMateria = false;
                        return;
                    }

                    else if (!RetrievingMateria && !ItemRightClicked && !RetrieveDialogOpened && HandleRightClickItem()) { return; }

                    else if (!RetrievingMateria && ItemRightClicked && !RetrieveDialogOpened && HandleContextMenuInteraction()) { return; }

                    else if (!RetrievingMateria && ItemRightClicked && RetrieveDialogOpened && HandleRetrieveDialog()) { return; }
                }

                if (materiaAffixed.Count() == 0 || CheckAllPreviousMateriaMatch(equippedItem, bis.BisItems?.FirstOrDefault(x => x.Id == (int)CurrentWorkingPieceId)?.Materia))
                {
                    PerformingAction = false;
                    FinishAutoUnmeld();
                }
            }

            if (AutoUnmelding && PerformingAction)
            {
                if (!ItemRightClicked && EzThrottler.Check("AutoUnMeld.RightClickItem") && EzThrottler.Check("AutoUnMeld.RetrievingMateria")) { PerformingAction = false; }
                if (ItemRightClicked && !RetrieveDialogOpened && EzThrottler.Check("AutoUnMeld.OpenRetrieveDialog") && EzThrottler.Check("AutoUnMeld.RetrievingMateria")) { PerformingAction = false; }
                if (ItemRightClicked && RetrieveDialogOpened && (EzThrottler.Check("AutoUnMeld.PreRetrieveCooldown") && EzThrottler.Check("AutoUnMeld.RetrievingMateria"))) { PerformingAction = false; }
            }

            if (AutoMelding && !PerformingAction)
            {
                PerformingAction = true;
                var equippedItem = CharacterInfo.GetEquippedItem((int)CurrentWorkingPieceId);
                var bis = P.Config.SavedBis?.Where(x => x.Job == SelectedWorkingJob && x.Name == SelectedWorkingBis).FirstOrDefault() ?? null;
                var bisMateriaCount = bis.BisItems?.FirstOrDefault(x => x.Id == (int)CurrentWorkingPieceId)?.Materia?.Count;
                var materiaAffixed = equippedItem->Materia.ToArray().Where(x => x != 0);

                if (materiaAffixed.Count() >= bisMateriaCount)
                {
                    FinishAutomeld();
                    return;
                }

                Svc.Log.Debug($"Item: {ItemSelected}, Materia: {MateriaSelected}, Melding: {AffixingMateria}");

                if (ItemSelected && MateriaSelected && AffixingMateria)
                {
                    ItemSelected = false;
                    MateriaSelected = false;
                    AffixingMateria = false;
                    return;
                }
                
                else if (!AffixingMateria && !ItemSelected && !MateriaSelected && HandleSelectItem()) { return; }

                else if (!AffixingMateria && ItemSelected && !MateriaSelected && HandleSelectMateriaToAffix()) { return; }

                else if (!AffixingMateria && ItemSelected && MateriaSelected && HandleConfirmMateriaMeld()) { return; }

                if (!Throttled) PerformingAction = false;
            }

            if (AutoMelding && PerformingAction)
            {
                if (!ItemSelected && EzThrottler.Check("AutoMeld.HandleSelectItem") && EzThrottler.Check("AutoMeld.AffixingMateria") && EzThrottler.Check("AutoUnMeld.RetrievingMateria")) { PerformingAction = false; }
                if (ItemSelected && !MateriaSelected && EzThrottler.Check("AutoMeld.HandleSelectMateria") && EzThrottler.Check("AutoMeld.AffixingMateria")) { PerformingAction = false; }
                if (ItemSelected && MateriaSelected && (EzThrottler.Check("AutoMeld.PreMeldCooldown") && EzThrottler.Check("AutoMeld.AffixingMateria"))) { PerformingAction = false; }
            }
        }

        private static bool HandleSelectItem()
        {
            const string Throttler = "AutoMeld.HandleSelectItem";
            if (!EzThrottler.Throttle("AutoUnMeld.RetrievingMateria")) { Svc.Log.Debug("Still unmelding..."); Throttled = true; return false; };
            if (!EzThrottler.Throttle("AutoMeld.AffixingMateria")) { Svc.Log.Debug("Still melding..."); Throttled = true; return false; };
            if (!EzThrottler.Throttle(Throttler, 750))
            {
                Throttled = true;
                return false;
            }

            Svc.Log.Debug($"Selecting Item from MateriaAttach addon.");
            PerformingAction = true;

            if(TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var materiaAttachAddon) && IsAddonReady(materiaAttachAddon))
            {
                var itemList = materiaAttachAddon->GetNodeById(13)->GetAsAtkComponentList();
                if (itemList != null && itemList->ListLength >= CurrentWorkingPieceIndex)
                {
                    ItemSelected = true;
                    Callback.Fire(materiaAttachAddon, true, 1, CurrentWorkingPieceIndex, 1, 0);
                    Svc.Log.Debug($"Selecting Item Index {CurrentWorkingPieceIndex}");

                    //EzThrottler.Throttle("AutoMeld.PreMateriaCooldown", 1250);
                    return true;
                } else { }

                //There has been an error we need to stop for here.
                FinishAutomeld();
                return false;
            }
            else if (materiaAttachAddon != null && !IsAddonReady(materiaAttachAddon)) { return true; }

            return false;
        }

        private static bool HandleSelectMateriaToAffix()
        {
            const string Throttler = "AutoMeld.HandleSelectMateria";
            if (!EzThrottler.Throttle(Throttler, 750))
            {
                Throttled = true;
                return false;
            }

            PerformingAction = true;
            Svc.Log.Debug($"Selecting Materia from List.");

            if (TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var materiaAttachAddon) && IsAddonReady(materiaAttachAddon))
            {
                var materiaList = materiaAttachAddon->GetNodeById(23)->GetAsAtkComponentNode();
                if (materiaList != null)
                {
                    var bis = P.Config.SavedBis?.Where(x => x.Job == SelectedWorkingJob && x.Name == SelectedWorkingBis).FirstOrDefault() ?? null;
                    if (bis != null)
                    {
                        var equippedItem = CharacterInfo.GetEquippedItem((int)CurrentWorkingPieceId);
                        if (equippedItem != null) 
                        {
                            var bisMateriaCount = bis.BisItems?.FirstOrDefault(x => x.Id == (int)CurrentWorkingPieceId)?.Materia?.Count;
                            var materiaAffixed = equippedItem->Materia.ToArray().Where(x => x != 0);

                            if (materiaAffixed.Count() >= bisMateriaCount || materiaAffixed.Count() > 4)
                            {
                                FinishAutomeld();
                                return false;
                            }

                            var nextMateria = bis.BisItems?.FirstOrDefault(x => x.Id == (int)CurrentWorkingPieceId)?.Materia?[materiaAffixed.Count()] ?? null;
                            if (nextMateria != null && nextMateria.Id != 0)
                            {
                                Svc.Log.Debug($"Finding Materia Index for {nextMateria.ItemName}");
                                var materiaNode = GetComponentNodeByMateriaName(materiaList, nextMateria.ItemName);
                                if (materiaNode > -1)
                                {
                                    MateriaSelected = true;
                                    
                                    //var reader = new ReaderMateriaAddon(materiaAttachAddon);

                                    Svc.Log.Debug($"Firing on 2, {materiaNode - 3}, 1, 0"); //I pulled these values from SimpleTweaks debugger.. I could cry
                                    Callback.Fire(materiaAttachAddon, true, 2, (materiaNode - 3), 1, 0);
                                    //CurrentWorkingPieceIndex = reader.SelectedItemIndex;

                                    EzThrottler.Throttle("AutoMeld.PreMeldCooldown", 750);

                                    return true;
                                }
                            }
                        }
                    }
                }

                //There has been an error we need to stop for here.
                FinishAutomeld();
                return false;
            }
            else if (materiaAttachAddon != null && !IsAddonReady(materiaAttachAddon)) { return true; }

            return false;
        }

        private static bool HandleConfirmMateriaMeld()
        { 
            const string Throttler = "AutoMeld.AffixingMateria";
            if (!EzThrottler.Check("AutoMeld.PreMeldCooldown")) return false;
            if (!EzThrottler.Throttle(Throttler, 4500))
            {
                Throttled = true;
                return false;
            }

            PerformingAction = true;
            
            if (TryGetAddonByName<AtkUnitBase>("MateriaAttachDialog", out var materiaAttachDialogAddon) && IsAddonReady(materiaAttachDialogAddon))
            {
                Svc.Log.Debug($"Affxing materia.");
                var materiaAttachDialog = new AddonMaster.MateriaAttachDialog(materiaAttachDialogAddon);
                materiaAttachDialog.MeldButton->ClickAddonButton(materiaAttachDialogAddon);
                AffixingMateria = true;

            }
            else if (materiaAttachDialogAddon != null && !IsAddonReady(materiaAttachDialogAddon)) { return true; }

            return false;
        }

        private static int GetComponentNodeByMateriaName(AtkComponentNode* materiaList, string materiaName)
        {
            try
            {
                var listItemCount = materiaList->GetComponent()->UldManager.NodeListCount;
                for (var i = 3; i < listItemCount - 2; i++)
                {
                    var listItem = materiaList->GetComponent()->UldManager.NodeList[i]->GetAsAtkComponentNode();
                    var materiaText = listItem->GetComponent()->UldManager.NodeList[5]->GetAsAtkTextNode()->NodeText.ExtractText();
                    Svc.Log.Debug($"Index {i} Materia Name: {materiaText}");
                    if (materiaName == materiaText) return i;
                }
            }
            catch (Exception e)
            {
            }

            return -1;
        }
    
        private static bool HandleRightClickItem()
        {
            const string Throttler = "AutoUnMeld.RightClickItem";
            if (!EzThrottler.Throttle(Throttler, 750))
            {
                Throttled = true;
                return false;
            }

            Svc.Log.Debug($"Right Clicking Item from MateriaAttach addon.");
            PerformingAction = true;

            if (TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var materiaAttachAddon) && IsAddonReady(materiaAttachAddon))
            {

                var itemList = materiaAttachAddon->GetNodeById(13)->GetAsAtkComponentList();
                if (itemList != null && itemList->ListLength >= CurrentWorkingPieceIndex)
                {
                    ItemSelected = true;
                    Callback.Fire(materiaAttachAddon, true, 4, CurrentWorkingPieceIndex, 0, 0);
                    Svc.Log.Debug($"Right Clicking Item Index {CurrentWorkingPieceIndex}");
                    ItemRightClicked = true;
                    return true;
                }
                else { }

                //There has been an error we need to stop for here.
                Abort();
                return false;
            }
            else if (materiaAttachAddon != null && !IsAddonReady(materiaAttachAddon)) { return true; }

            return false;
        }

        private static bool HandleContextMenuInteraction()
        {
            const string Throttler = "AutoMeld.OpenRetrieveDialog";
            if (!EzThrottler.Throttle(Throttler, 750))
            {
                Throttled = true;
                return false;
            }

            PerformingAction = true;

            if (TryGetAddonByName<AtkUnitBase>("ContextMenu", out var contextMenu) && IsAddonReady(contextMenu))
            {
                Callback.Fire(contextMenu, true, 0, 1, 0);
                EzThrottler.Throttle("AutoUnMeld.PreRetrieveCooldown");
                RetrieveDialogOpened = true;
            }
            else if (contextMenu != null && !IsAddonReady(contextMenu)) { return true; }

            return false;
        }

        private static bool HandleRetrieveDialog()
        {
            const string Throttler = "AutoUnMeld.RetrievingMateria";
            if (!EzThrottler.Check("AutoUnMeld.PreRetrieveCooldown")) return false;
            if (!EzThrottler.Throttle(Throttler, 4500))
            {
                Throttled = true;
                return false;
            }

            PerformingAction = true;

            if (TryGetAddonByName<AtkUnitBase>("MateriaRetrieveDialog", out var materiaRetrieveDialogAddon) && IsAddonReady(materiaRetrieveDialogAddon))
            {
                Svc.Log.Debug($"Affxing materia.");
                var materiaRetrieveDialog = new AddonMaster.MateriaRetrieveDialog(materiaRetrieveDialogAddon);
                materiaRetrieveDialog.BeginButton->ClickAddonButton(materiaRetrieveDialogAddon);
                RetrievingMateria = true;

            }
            else if (materiaRetrieveDialogAddon != null && !IsAddonReady(materiaRetrieveDialogAddon)) { return true; }

            return false;
        }

        public static unsafe bool CheckAllPreviousMateriaMatch(InventoryItem* item, List<JobBis_ItemMateria>? materia)
        {
            var itemMelds = item->Materia.ToArray();
            var bisMelds = materia;

            for (var i = bisMelds.Count() - 1; i > -1; i--)
            {
                var bisMateriaId = bisMelds[i] != null ? LuminaSheets.GetMateriaSheetIdFromMateriaItemId(bisMelds[i].Id) : 0;
                Svc.Log.Debug($"[Meld Index {i}] Affixed: {itemMelds[i]} | Bis: {bisMateriaId}");

                if (itemMelds[i] == 0) continue;
                if (itemMelds[i] != bisMateriaId)
                    return false;
            }

            return true;
        }

        public static void StartAutoUnmeld()
        {
            if (CurrentWorkingPieceIndex == 0) return;
            if (CurrentWorkingPieceId == 0) return;
            if (SelectedWorkingJob == 0) return;
            if (SelectedWorkingBis == string.Empty) return;

            AutoUnmelding = true;
            P.MeldUI.SetAutomeld();
        }

        public static void FinishAutoUnmeld()
        {
            Throttled = false;
            ItemRightClicked = false;
            RetrieveDialogOpened = false;
            RetrievingMateria = false;

            ItemSelected = false;
            AutoUnmelding = false;
            StartAutomeld();
        }

        public static void StartAutomeld()
        {
            if (CurrentWorkingPieceIndex == 0) return;
            if (CurrentWorkingPieceId == 0) return;
            if (SelectedWorkingJob == 0) return;
            if (SelectedWorkingBis == string.Empty) return;

            AutoMelding = true;
            P.MeldUI.SetAutomeld();
        }

        public static void FinishAutomeld()
        {
            PerformingAction = false;
            Svc.Log.Debug($"Finished AutoMeld Operation.");
            CurrentWorkingPieceIndex = 0;
            CurrentWorkingPieceId = 0;
            SelectedWorkingJob = 0;
            SelectedWorkingBis = string.Empty;

            ItemSelected = false;
            Throttled = false;
            MateriaSelected = false;
            AffixingMateria = false;

            AutoMelding = false;
            P.MeldUI.EndAutomeld();
        }

        public static void Abort()
        {
            Svc.Log.Debug($"ABORT");
            PerformingAction = false;
            Aborting = true;

            Throttled = false;
            ItemRightClicked = false;
            RetrieveDialogOpened = false;
            RetrievingMateria = false;

            CurrentWorkingPieceIndex = 0;
            CurrentWorkingPieceId = 0;
            SelectedWorkingJob = 0;
            SelectedWorkingBis = string.Empty;

            ItemSelected = false;
            Throttled = false;
            MateriaSelected = false;
            AffixingMateria = false;

            AutoUnmelding = false;
            AutoMelding = false;
            P.MeldUI.EndAutomeld();

            Aborting = false;
        }
    }
}
