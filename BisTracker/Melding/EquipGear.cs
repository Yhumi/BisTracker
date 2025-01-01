using BisTracker.RawInformation;
using BisTracker.RawInformation.Character;
using Dalamud.Game.Network.Structures;
using Dalamud.Hooking;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.Melding
{
    public unsafe static class EquipGear
    {
        public enum TaskResult { Done, Retry, Skip, Abort }
        public enum EquipGearStatus { Idle, Working }

        public static List<(Func<TaskResult> task, TimeSpan retryDelay)> Tasks = new();
        private static DateTime NextRetry;
        public static EquipGearStatus Status;

        private delegate void* FireCallbackDelegate(AtkUnitBase* atkUnitBase, int valueCount, AtkValue* atkValues, byte updateVisibility);
        private static Hook<FireCallbackDelegate> _fireCallbackHook;

        public static int EquipGearsetLoops = 0;
        public static int EquipAttemptLoops = 0;

        static EquipGear()
        {
            _fireCallbackHook = Svc.Hook.HookFromSignature<FireCallbackDelegate>("E8 ?? ?? ?? ?? 0F B6 E8 8B 44 24 20", CallbackDetour);
        }

        private static void* CallbackDetour(AtkUnitBase* atkUnitBase, int valueCount, AtkValue* atkValues, byte updateVisibility)
        {
            var name = atkUnitBase->NameString.TrimEnd();
            if (name.Substring(0, 11) == "SelectYesno")
            {
                var result = atkValues[0];
                if (result.Int == 1)
                {
                    Svc.Log.Debug($"Select no, clearing tasks");
                    Tasks.Clear();
                }

                _fireCallbackHook.Disable();

            }
            return _fireCallbackHook.Original(atkUnitBase, valueCount, atkValues, updateVisibility);
        }

        public static void Update()
        {
            if (DateTime.Now < NextRetry)
                return;

            while (Tasks.Count > 0)
            {
                Status = EquipGearStatus.Working;
                switch (Tasks[0].task())
                {
                    case TaskResult.Done:
                    case TaskResult.Skip:
                        EquipGearsetLoops = 0;
                        EquipAttemptLoops = 0;
                        Tasks.RemoveAt(0);
                        break;
                    case TaskResult.Retry:
                        NextRetry = DateTime.Now.Add(Tasks[0].retryDelay);
                        return;
                    case TaskResult.Abort:
                        Tasks.Clear();
                        Status = EquipGearStatus.Idle;
                        return;
                }
            }

            Status = EquipGearStatus.Idle;
        }

        public static TaskResult EquipGearset(Job job)
        {
            if (job == CharacterInfo.JobID)
                return TaskResult.Done;

            if (EquipGearsetLoops >= 5)
            {
                Svc.Log.Debug("[AutoEquip] Could not equip gearset. Continuing to manual gear equip attempts.");
                return TaskResult.Skip;
            }

            var gearsets = RaptureGearsetModule.Instance();
            foreach (var gs in gearsets->Entries)
            {
                if (!RaptureGearsetModule.Instance()->IsValidGearset(gs.Id)) continue;
                if ((Job)gs.ClassJob == job)
                {
                    if (gs.Flags.HasFlag(RaptureGearsetModule.GearsetFlag.MainHandMissing))
                    {
                        if (TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var selectyesno))
                        {
                            if (selectyesno->AtkUnitBase.IsVisible)
                                return TaskResult.Retry;
                        }
                        else
                        {
                            EquipGearsetLoops++;
                            _fireCallbackHook?.Enable();
                            var r = gearsets->EquipGearset(gs.Id);
                            return r < 0 ? TaskResult.Abort : TaskResult.Retry;
                        }
                    }

                    var result = gearsets->EquipGearset(gs.Id);
                    EquipGearsetLoops++;
                    Svc.Log.Debug($"Tried to equip gearset {gs.Id} for {job}, result={result}, flags={gs.Flags}");
                    return result < 0 ? TaskResult.Abort : TaskResult.Retry;
                }
            }

            Svc.Log.Debug($"No gearsets found for {job}. Continuing to manual gear equip attempts.");
            return TaskResult.Skip;
        }

        public static TaskResult EquipGearPiece(int itemId)
        {
            //Gear equipped.
            if (CharacterInfo.GetEquippedItem(itemId) != null)
                return TaskResult.Done;
                
            //Alright, lets equip it.
            var item = LuminaSheets.ItemSheet[(uint)itemId];

            var gearSlot = CharacterInfo.GetSlotIndexFromEquipSlotCategory(item.EquipSlotCategory.Value);
            if (gearSlot == null) return TaskResult.Skip;

            var armourChestInv = CharacterInfo.GetArmouryChestInventoryTypeFromGearSlotIndex(gearSlot.Value);
            var itemIndexInInventory = CharacterInfo.GetInventoryItemSlot(InventoryManager.Instance()->GetInventoryContainer(armourChestInv), itemId);
            if (itemIndexInInventory > -1)
            {
                Svc.Log.Debug($"{item.Name} found in armoury chest. Attempting to equip.");
                AgentInventoryContext.Instance()->OpenForItemSlot(armourChestInv, itemIndexInInventory, (uint)AgentId.ArmouryBoard);

                var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu");
                if (contextMenu != null)
                {
                    for (int i = 0; i < contextMenu->AtkValuesCount; i++)
                    {
                        var firstEntryIsEquip = AgentInventoryContext.Instance()->EventIds[i] == 25; // i'th entry will fire eventid 7+i; eventid 25 is 'equip'
                        if (firstEntryIsEquip)
                        {
                            Svc.Log.Debug($"Equipping item #{itemId} from {armourChestInv} @ {itemIndexInInventory}, index {i}");
                            Callback.Fire(contextMenu, true, 0, i - 7, 0, 0, 0); // p2=-1 is close, p2=0 is exec first command
                        }
                    }
                    Callback.Fire(contextMenu, true, 0, -1, 0, 0, 0);
                    EquipAttemptLoops++;

                    if (EquipAttemptLoops >= 5)
                    {
                        Svc.Log.Debug($"Equip option not found after 5 attempts. Skipping.");
                        return TaskResult.Skip;
                    }
                }
                return TaskResult.Retry;
            }

            var findInMainInventory = CharacterInfo.GetInventoryItemPositon(itemId);
            if (findInMainInventory != null)
            {
                Svc.Log.Debug($"{item.Name} found in Inventory. Attempting to equip.");
                AgentInventoryContext.Instance()->OpenForItemSlot(findInMainInventory.Value.inventory, findInMainInventory.Value.pos, (uint)AgentId.Inventory);

                var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu");
                if (contextMenu != null)
                {
                    for (int i = 0; i < contextMenu->AtkValuesCount; i++)
                    {
                        var firstEntryIsEquip = AgentInventoryContext.Instance()->EventIds[i] == 25; // i'th entry will fire eventid 7+i; eventid 25 is 'equip'
                        if (firstEntryIsEquip)
                        {
                            Svc.Log.Debug($"Equipping item #{itemId} from {findInMainInventory.Value.inventory} @ {findInMainInventory.Value.pos}, index {i}");
                            Callback.Fire(contextMenu, true, 0, i - 7, 0, 0, 0); // p2=-1 is close, p2=0 is exec first command
                        }
                    }
                    Callback.Fire(contextMenu, true, 0, -1, 0, 0, 0);
                    EquipAttemptLoops++;

                    if (EquipAttemptLoops >= 5)
                    {
                        Svc.Log.Debug($"Equip option not found after 5 attempts. Skipping.");
                        return TaskResult.Skip;
                    }
                }
                return TaskResult.Retry;
            }

            Svc.Log.Debug($"{item.Name} not found in inventory. Skipping.");
            return TaskResult.Skip;
        }
        
        public static void Dispose()
        {
            _fireCallbackHook?.Dispose();
        }
    }
}
