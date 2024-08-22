using BisTracker.RawInformation;
using BisTracker.RawInformation.Character;
using BisTracker.Readers;
using ECommons.Automation;
using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
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

        internal static bool AutoMelding = false;
        internal static bool PerformingAction = false;
        internal static bool Throttled = false;
        internal static bool Initialised = false;

        internal static bool ItemSelected = false;
        internal static bool MateriaSelected = false;
        internal static bool AffixingMateria = false;

        internal static bool Errors = false;

        public static void Init()
        {
            AutoMelding = false;
            Initialised = true;
        }

        public static void Tick()
        {
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
                if (!ItemSelected && EzThrottler.Check("AutoMeld.HandleSelectItem") && EzThrottler.Check("AutoMeld.AffixingMateria")) { PerformingAction = false; }
                if (ItemSelected && !MateriaSelected && EzThrottler.Check("AutoMeld.HandleSelectMateria") && EzThrottler.Check("AutoMeld.AffixingMateria")) { PerformingAction = false; }
                if (ItemSelected && MateriaSelected && (EzThrottler.Check("AutoMeld.PreMeldCooldown") && EzThrottler.Check("AutoMeld.AffixingMateria"))) { PerformingAction = false; }
            }
        }

        private static bool HandleSelectItem()
        {
            const string Throttler = "AutoMeld.HandleSelectItem";
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
                    return true;
                } else { }
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
                                    
                                    var reader = new ReaderMateriaAddon(materiaAttachAddon);

                                    Svc.Log.Debug($"Firing on 2, {materiaNode - 3}, 1, 0"); //I pulled these values from SimpleTweaks debugger.. I could cry
                                    Callback.Fire(materiaAttachAddon, true, 2, (materiaNode - 3), 1, 0);
                                    CurrentWorkingPieceIndex = reader.SelectedItemIndex;

                                    EzThrottler.Throttle("AutoMeld.PreMeldCooldown", 2000);

                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            else if (materiaAttachAddon != null && !IsAddonReady(materiaAttachAddon)) { return true; }

            return false;
        }

        private static bool HandleConfirmMateriaMeld()
        { 
            const string Throttler = "AutoMeld.AffixingMateria";
            if (!EzThrottler.Check("AutoMeld.PreMeldCooldown")) return false;
            if (!EzThrottler.Throttle(Throttler, 7000))
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
    
        public static void StartAutomeld()
        {
            if (CurrentWorkingPieceIndex == 0) return;
            if (CurrentWorkingPieceId == 0) return;
            if (SelectedWorkingJob == 0) return;
            if (SelectedWorkingBis == string.Empty) return;

            AutoMelding = true;
        }

        public static void FinishAutomeld()
        {
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
        }
    }
}
