using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.RawInformation
{
    public class LuminaSheets
    {
        public static Dictionary<uint, ClassJob>? ClassJobSheet;
        public static Dictionary<uint, Item>? ItemSheet;
        public static Dictionary<uint, Materia>? MateriaSheet;

        public static void Init()
        {
            ClassJobSheet = Svc.Data?.GetExcelSheet<ClassJob>()?
                           .ToDictionary(i => i.RowId, i => i);

            ItemSheet = Svc.Data?.GetExcelSheet<Item>()?
                       .ToDictionary(i => i.RowId, i => i);

            MateriaSheet = Svc.Data?.GetExcelSheet<Materia>()?
                        .ToDictionary(i => i.RowId, i => i);
                       
        }

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
