using BisTracker.BiS;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.RawInformation.Character
{
    public static unsafe class CharacterInfo
    {
        public static unsafe void UpdateCharaStats(uint? classJobId = null)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            JobID = (Job)(classJobId ?? Svc.ClientState.LocalPlayer?.ClassJob.Id ?? 0);
            JobIDUint = classJobId ?? Svc.ClientState.LocalPlayer?.ClassJob.Id ?? 0;
            CharacterLevel = Svc.ClientState.LocalPlayer?.Level;
        }

        public static unsafe (uint tomestoneItemId, uint tomestoneCount) GetPlayerTomestoneFromShop(int shopTomestoneCostId)
        {
            //Svc.Log.Debug($"Checking for Shop TomestoneId: {shopTomestoneCostId}");
            var tomestone = LuminaSheets.TomestonesItemSheet?.FirstOrDefault(x => x.Value.Tomestones.Row == shopTomestoneCostId);
            if (!tomestone.HasValue) return (0, 0);

            return (tomestone.Value.Value.Item.Row, InventoryManager.Instance()->GetTomestoneCount(tomestone.Value.Value.Item.Row));
        }

        public static unsafe void SetCharaInventoryPointers()
        {
            EquippedGear = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
        }

        public static unsafe InventoryItem* SearchForItemInArmouryChest(int itemId, CharacterEquippedGearSlotIndex gearSlot)
        {
            switch (gearSlot)
            {
                case CharacterEquippedGearSlotIndex.MainHand:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryMainHand), itemId);

                case CharacterEquippedGearSlotIndex.OffHand:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryOffHand), itemId);

                case CharacterEquippedGearSlotIndex.Head:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryHead), itemId);

                case CharacterEquippedGearSlotIndex.Body:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryBody), itemId);

                case CharacterEquippedGearSlotIndex.Gloves:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryHands), itemId);

                case CharacterEquippedGearSlotIndex.Legs:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryLegs), itemId);

                case CharacterEquippedGearSlotIndex.Feet:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryFeets), itemId);

                case CharacterEquippedGearSlotIndex.Ears:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryEar), itemId);

                case CharacterEquippedGearSlotIndex.Neck:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryNeck), itemId);

                case CharacterEquippedGearSlotIndex.Wrists:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryWrist), itemId);

                case CharacterEquippedGearSlotIndex.RightRing:
                case CharacterEquippedGearSlotIndex.LeftRing:
                    return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.ArmoryRings), itemId);

                default:
                    return null;
            }
        }

        public static unsafe InventoryItem* SearchForItemInPlayerInventory(int itemId)
        {
            var inv1Item = SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1), itemId);
            if (inv1Item != null) return inv1Item;

            var inv2Item = SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2), itemId);
            if (inv2Item != null) return inv2Item;

            var inv3Item = SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3), itemId);
            if (inv3Item != null) return inv3Item;

            return SearchForItemInInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4), itemId);
        }

        public static unsafe InventoryItem* GetEquippedItem(int itemId)
        {
            var equipSlotCategory = LuminaSheets.ItemSheet[(uint)itemId].EquipSlotCategory.Value;
            if (equipSlotCategory == null) return null;

            var equippedSlot = GetSlotIndexFromEquipSlotCategory(equipSlotCategory);
            if (equippedSlot == null) return null;

            var equippedItem = EquippedGear->GetInventorySlot((int)equippedSlot);
            if (equippedItem != null && equippedItem->ItemId == itemId) return equippedItem;

            if (equippedSlot == CharacterEquippedGearSlotIndex.RightRing)
            {
                var otherRing = EquippedGear->GetInventorySlot((int)CharacterEquippedGearSlotIndex.LeftRing);
                if (otherRing->ItemId == itemId) return otherRing;
            }

            if (equippedSlot == CharacterEquippedGearSlotIndex.LeftRing)
            {
                var otherRing = EquippedGear->GetInventorySlot((int)CharacterEquippedGearSlotIndex.RightRing);
                if (otherRing->ItemId == itemId) return otherRing;
            }

            return null;
        }

        private static unsafe InventoryItem* SearchForItemInInventory(InventoryContainer* inv, int itemId)
        {
            uint invSize = inv->Size;
            for (int i = 0; i < invSize; i++)
            {
                if (inv->GetInventorySlot(i)->ItemId == itemId)
                    return inv->GetInventorySlot(i);
            }

            return null;
        }

        public static unsafe InventoryItem* FindInventoryItem(int itemId)
        {
            //Firstly check for equipped item
            var invItem = GetEquippedItem(itemId);
            if (invItem != null && invItem->ItemId == itemId) 
                return invItem;

            //Now check in AC
            var equipSlotCategory = LuminaSheets.ItemSheet[(uint)itemId].EquipSlotCategory.Value;
            if (equipSlotCategory == null) 
                return null;

            var equippedSlot = GetSlotIndexFromEquipSlotCategory(equipSlotCategory);
            if (equippedSlot == null) 
                return null;

            var acItem = SearchForItemInArmouryChest(itemId, equippedSlot.Value);
            if (acItem != null)
                return acItem;

            //Finally check in inv itself
            return SearchForItemInPlayerInventory(itemId);
        }

        public static unsafe InventoryItem* FindUnaugmentedVersionOfAugmentedTomeItem(int itemId)
        {
            var isAugmentedItem = LuminaSheets.ItemSheet[(uint)itemId].Name.ExtractText().ToLower().Contains("augmented");

            //Dont really need to do anything fancy if its the unaugmented version, we'd just find that.
            //I cant imagine a bis that uses unaugmented versions but people have the aug version laying around. Maybe I account for that in the future.
            if (!isAugmentedItem) return FindInventoryItem(itemId); 

            var unaugmentedName = LuminaSheets.ItemSheet[(uint)itemId].Name.ExtractText().Replace("Augmented", "").Trim();
            var unaugmentedItem = LuminaSheets.ItemSheet.Values.Where(x => x.Name.ExtractText().ToLower() == unaugmentedName.ToLower()).FirstOrDefault();
            if (unaugmentedItem == null) return null;

            return FindInventoryItem((int)unaugmentedItem.RowId);
        }

        public static unsafe int GetInventoryItemSlot(InventoryContainer* inv, int itemId)
        {
            uint invSize = inv->Size;
            for (int i = 0; i < invSize; i++)
            {
                if (inv->GetInventorySlot(i)->ItemId == itemId)
                    return i;
            }

            return -1;
        }

        public static unsafe InventoryType GetArmouryChestInventoryTypeFromGearSlotIndex(CharacterEquippedGearSlotIndex gearSlot)
        {
            switch (gearSlot)
            {
                case CharacterEquippedGearSlotIndex.MainHand:
                    return InventoryType.ArmoryMainHand;
                case CharacterEquippedGearSlotIndex.LeftRing:
                case CharacterEquippedGearSlotIndex.RightRing:
                    return InventoryType.ArmoryRings;
                default:
                    return (InventoryType)((int)gearSlot + 3199);
            }
        }

        public static CharacterEquippedGearSlotIndex? GetSlotIndexFromEquipSlotCategory(EquipSlotCategory? category)
        {
            if (category == null) return null;
            if (category.MainHand == 1) return CharacterEquippedGearSlotIndex.MainHand;
            if (category.OffHand == 1) return CharacterEquippedGearSlotIndex.OffHand;
            if (category.Head == 1) return CharacterEquippedGearSlotIndex.Head;
            if (category.Body == 1) return CharacterEquippedGearSlotIndex.Body;
            if (category.Gloves == 1) return CharacterEquippedGearSlotIndex.Gloves;
            if (category.Waist == 1) return CharacterEquippedGearSlotIndex.Waist;
            if (category.Legs == 1) return CharacterEquippedGearSlotIndex.Legs;
            if (category.Feet == 1) return CharacterEquippedGearSlotIndex.Feet;
            if (category.Ears == 1) return CharacterEquippedGearSlotIndex.Ears;
            if (category.Neck == 1) return CharacterEquippedGearSlotIndex.Neck;
            if (category.Wrists == 1) return CharacterEquippedGearSlotIndex.Wrists;
            if (category.FingerR == 1) return CharacterEquippedGearSlotIndex.RightRing;
            if (category.FingerL == 1) return CharacterEquippedGearSlotIndex.LeftRing;
            if (category.SoulCrystal == 1) return CharacterEquippedGearSlotIndex.SoulCrystal;
            return null;
        }

        public static unsafe (InventoryType inventory, int pos)? GetInventoryItemPositon(int itemId)
        {
            var inv1Item = GetInventoryItemSlot(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1), itemId);
            if (inv1Item > -1) return (InventoryType.Inventory1, inv1Item);

            var inv2Item = GetInventoryItemSlot(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2), itemId);
            if (inv2Item > -1) return (InventoryType.Inventory2, inv2Item);

            var inv3Item = GetInventoryItemSlot(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3), itemId);
            if (inv3Item > -1) return (InventoryType.Inventory3, inv3Item);

            var inv4Item = GetInventoryItemSlot(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4), itemId);
            if (inv4Item > -1) return (InventoryType.Inventory4, inv4Item);

            return null;
        }

        public static byte? CharacterLevel;
        public static Job JobID;
        public static uint JobIDUint;

        public static Dictionary<uint, uint> PlayerTomestones = new();

        public static InventoryContainer* EquippedGear;
    }

    public enum CharacterEquippedGearSlotIndex
    {
        MainHand = 0,
        OffHand = 1,
        Head = 2,
        Body = 3,
        Gloves = 4,
        Waist = 5,
        Legs = 6,
        Feet = 7,
        Ears = 8,
        Neck = 9,
        Wrists = 10,
        RightRing = 11,
        LeftRing = 12,
        SoulCrystal = 13
    }
}
