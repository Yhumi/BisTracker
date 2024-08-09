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

        public string Name { 
            get
            {
                switch(SheetType)
                {
                    case (BisSheetType)1:
                        return SelectedXivGearAppSet ?? "";
                    case null:
                    default:
                        return "";
                }
            } 
        }

        //XivGearAppSpecific
        public string? SelectedXivGearAppSet { get; set; }
        public XivGearAppResponse? XivGearAppResponse { get; set; }
    }
}
