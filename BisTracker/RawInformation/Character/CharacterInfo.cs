using ECommons.DalamudServices;
using ECommons.ExcelServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.RawInformation.Character
{
    public static class CharacterInfo
    {
        public static unsafe void UpdateCharaStats(uint? classJobId = null)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            JobID = (Job)(classJobId ?? Svc.ClientState.LocalPlayer?.ClassJob.Id ?? 0);
            JobIDUint = classJobId ?? Svc.ClientState.LocalPlayer?.ClassJob.Id ?? 0;
            CharacterLevel = Svc.ClientState.LocalPlayer?.Level;
        }

        public static byte? CharacterLevel;
        public static Job JobID;
        public static uint JobIDUint;
    }
}
