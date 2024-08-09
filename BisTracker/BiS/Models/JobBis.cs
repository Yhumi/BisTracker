using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.BiS.Models
{
    public class JobBis
    {
        public uint? Job { get; set; }
        public string? Fight { get; set; }
        public string? Link { get; set; }
        public BisSheetType? SheetType { get; set; }
        public string? Name { get; set; }

        //XivGearAppSpecific
        public string? SelectedXivGearAppSet { get; set; }
        public int? Food { get; set; }
        public XivGearApp_SetItems? XivGearAppSetItems { get; set; }
    }
}
