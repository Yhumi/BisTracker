using BisTracker.BiS.Models;
using BisTracker.RawInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;

namespace BisTracker.BiS
{
    internal class BisItem
    {
        public uint Id { get; set; }
        public string? ItemName { get; set; }
        public List<BisMateria> Materia { get; set; }

        public BisItem() { }
        public BisItem(uint id, uint[]? materiaIds = null)
        {
            Id = id;
            var item = LuminaSheets.ItemSheet[id];
            ItemName = item.Name.ExtractText();

            if (materiaIds != null)
            {
                Materia = new();
                foreach (var mat in materiaIds)
                {
                    Materia.Add(new BisMateria(mat));
                }
            }
        }

        public BisItem(JobBis_Item jobBisItem)
        {
            Id = (uint)jobBisItem.Id;
            var item = LuminaSheets.ItemSheet[(uint)jobBisItem.Id];
            ItemName = item.Name.ExtractText();

            if (jobBisItem.Materia != null)
            {
                Materia = new();
                foreach (var mat in jobBisItem.Materia.Where(x => x.Id > -1))
                {
                    Materia.Add(new BisMateria(mat));
                }
            }
        }
    }

    internal class BisMateria
    {
        public uint Id { get; set; }
        public string? ItemName { get; set; }

        public BisMateria() { }
        public BisMateria(uint id)
        {
            Id = id;
            ItemName = LuminaSheets.ItemSheet[id].Name.ExtractText();
        }
        public BisMateria(JobBis_ItemMateria materia)
        {
            Id = (uint)materia.Id;
            ItemName = materia.GetMateriaLabel();
        }
    }
}
