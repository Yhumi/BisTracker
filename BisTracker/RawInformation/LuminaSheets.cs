using BisTracker.RawInformation.Character;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Lumina.Excel.GeneratedSheets2.SpecialShop;

namespace BisTracker.RawInformation
{
    public class LuminaSheets
    {
        public static Dictionary<uint, ClassJob>? ClassJobSheet;
        public static Dictionary<uint, Item>? ItemSheet;
        public static Dictionary<uint, ItemFood>? ItemFoodSheet;
        public static Dictionary<uint, Materia>? MateriaSheet;
        public static Dictionary<uint, BaseParam>? BaseParamSheet;
        public static Dictionary<uint, SpecialShop>? SpecialShopSheet;

        public static void Init()
        {
            ClassJobSheet = Svc.Data?.GetExcelSheet<ClassJob>()?
                           .ToDictionary(i => i.RowId, i => i);

            ItemSheet = Svc.Data?.GetExcelSheet<Item>()?
                       .ToDictionary(i => i.RowId, i => i);

            ItemFoodSheet = Svc.Data?.GetExcelSheet<ItemFood>()?
                       .ToDictionary(i => i.RowId, i => i);

            MateriaSheet = Svc.Data?.GetExcelSheet<Materia>()?
                        .ToDictionary(i => i.RowId, i => i);

            BaseParamSheet = Svc.Data?.GetExcelSheet<BaseParam>()?
                        .ToDictionary(i => i.RowId, i => i);

            SpecialShopSheet = Svc.Data?.GetExcelSheet<SpecialShop>()?
                        .ToDictionary(i => i.RowId, i => i);
        }

        public static Materia? GetMateriaFromSpecificMateria(int materiaId)
        {
            var materiaItem = ItemSheet[(uint)materiaId];
            if (materiaItem == null) return null;

            var materia = MateriaSheet.Where(x => x.Value != null && x.Value.Item.Where(y => y.Value != null).Select(y => y.Value!.RowId).Contains((uint)materiaId)).FirstOrDefault();
            return materia.Value ?? null;
        }

        public static Item? GetItemFromItemFoodRowId(int itemFoodId)
        {
            var item = ItemSheet.Where(x => x.Value.ItemAction.Value.Data[1] == itemFoodId).FirstOrDefault().Value;
            return item;
        }

        public static int? GetMaxStatForItem(uint itemId, uint paramId)
        {
            Item? item = ItemSheet?[itemId];
            if (item == null) return null;
            BaseParam? baseParam = BaseParamSheet?[paramId];
            if (baseParam == null) return null;
            if (item.ClassJobUse.Value == null || item.EquipSlotCategory.Value == null) return null;
            if (item.BaseParamModifier >= baseParam.MeldParam.Length) return null;
            if (item.LevelItem.Value == null) return null;

            PropertyInfo[] properties = typeof(ItemLevel).GetProperties();
            var baseValProp = properties.Where(x => x.Name.ToLower() == baseParam.Name.ExtractText().Replace(" ", "").ToLower()).FirstOrDefault();
            if (baseValProp == null) return null;

            var baseVal = baseValProp.GetValue(item.LevelItem.Value);
            if (baseVal == null) return null;
            
            var slotModifier = GetPercentageForItemSlot(baseParam, item.ClassJobUse.Value, item.EquipSlotCategory.Value);
            if (slotModifier == null) return null;

            var baseParamMeldModifier = baseParam.MeldParam[item.BaseParamModifier];
            return (int) Math.Round((ushort) baseVal * slotModifier.Value / (baseParamMeldModifier * 10d));
        }

        public static ushort? GetPercentageForItemSlot(BaseParam param, ClassJob job, EquipSlotCategory equipSlotCategory)
        {
            CharacterEquippedGearSlotIndex? characterEquippedGearSlotIndex = CharacterInfo.GetSlotIndexFromEquipSlotCategory(equipSlotCategory);
            if (characterEquippedGearSlotIndex == null) return null;

            bool canEquipOffHand = IsJobTwoHanded(job);

            switch (characterEquippedGearSlotIndex.Value) {
                case CharacterEquippedGearSlotIndex.MainHand:
                    return canEquipOffHand ? param.OneHandWeaponPercent : param.TwoHandWeaponPercent;
                case CharacterEquippedGearSlotIndex.OffHand:
                    return param.OffHandPercent;
                case CharacterEquippedGearSlotIndex.Head:
                    return param.HeadPercent;
                case CharacterEquippedGearSlotIndex.Body:
                    return param.ChestPercent;
                case CharacterEquippedGearSlotIndex.Gloves:
                    return param.HandsPercent;
                case CharacterEquippedGearSlotIndex.Legs:
                    return param.LegsPercent;
                case CharacterEquippedGearSlotIndex.Feet:
                    return param.FeetPercent;
                case CharacterEquippedGearSlotIndex.Ears:
                    return param.EarringPercent;
                case CharacterEquippedGearSlotIndex.Neck:
                    return param.NecklacePercent;
                case CharacterEquippedGearSlotIndex.Wrists:
                    return param.BraceletPercent;
                case CharacterEquippedGearSlotIndex.RightRing:
                case CharacterEquippedGearSlotIndex.LeftRing:
                    return param.RingPercent;
                default:
                    return null;
            }
        }

        public static bool IsJobTwoHanded(ClassJob job)
        {
            return ConstantData.TwoHandedJobs.Contains(job.Abbreviation.ToString().ToUpper());
        }

        //public static string GetSpecialShopContainingItem(int itemId)
        //{
        //    var specialShopItems = SpecialShopSheet.Values.SelectMany(x => x.Item).Where(x => x.Item.Any(y => y.Value.RowId == itemId));
        //    return specialShopItems.FirstOrDefault().Category.FirstOrDefault()?.Value?.Name.ExtractText() ?? string.Empty;  
        //}

        public static void Dispose()
        {
            var type = typeof(LuminaSheets);
            foreach (var prop in type.GetFields(System.Reflection.BindingFlags.Static))
            {
                prop.SetValue(null, null);
            }
        }
    }    
}
