using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.Linq;
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
