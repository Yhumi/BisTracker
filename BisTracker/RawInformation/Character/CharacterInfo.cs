using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;
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

        public static unsafe void SetCharaInventoryPointers()
        {
            EquippedGear = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
        }

        public static unsafe int? SearchForItemInArmouryChest(int itemId, CharacterEquippedGearSlotIndex gearSlot)
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

        private static unsafe int? SearchForItemInInventory(InventoryContainer* inv, int itemId)
        {
            uint invSize = inv->Size;
            for (int i = 0; i < invSize; i++)
            {
                if (inv->GetInventorySlot(i)->ItemId == itemId)
                    return i;
            }

            return null;
        }

        public static unsafe Span<ushort> GetItemMateria(CharacterEquippedGearSlotIndex index) => GetInventoryItem(index)->Materia;

        public static unsafe InventoryItem* GetInventoryItem(CharacterEquippedGearSlotIndex index) => EquippedGear->GetInventorySlot((int) index);

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

        public static byte? CharacterLevel;
        public static Job JobID;
        public static uint JobIDUint;

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
