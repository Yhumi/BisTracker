using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.Melding
{
    public unsafe static class MeldingAddonUtils
    {
        public static bool IsMeldingMenuOpen() => Svc.GameGui.GetAddonByName("MateriaAttach", 1) != IntPtr.Zero;

        public unsafe static bool OpenMateriaMelder()
        {
            if (!IsMeldingMenuOpen())
            {
                ActionManager.Instance()->UseAction(ActionType.GeneralAction, 13);
            }
            return true;
        }

        public unsafe static bool SelectEquippedItemsDropdown()
        {
            if (TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var materiaAttach))
            {
                Callback.Fire(materiaAttach, true, 0, 6);
                return true;
            }
            return false;
        }
    }
}
