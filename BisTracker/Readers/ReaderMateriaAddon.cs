using ECommons.UIHelpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.Readers
{
    public unsafe class ReaderMateriaAddon(AtkUnitBase* Addon) : AtkReader(Addon)
    {
        public int SelectedItemIndex => ReadInt(287) ?? 0;
        public int ValidMateriaCount => ReadInt(288) ?? 0;

        public List<MateriaNames> MateriaNameList => Loop<MateriaNames>(429, 1, (int)ValidMateriaCount);

        public unsafe class Item(nint Addon, int start) : AtkReader(Addon, start)
        {

        }

        public unsafe class MateriaNames(nint Addon, int start) : AtkReader(Addon, start)
        {
            public string Name => ReadString(0);
        }
    }
}
