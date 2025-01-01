using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.BiS.Models
{
    public class XivGearAppResponse
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("sets")]
        public XivGearApp_Set[]? Sets { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("items")]
        public XivGearApp_SetItems? Items { get; set; }

        [JsonProperty("food")]
        public int? Food { get; set; }

        public bool Error = false;

        public XivGearAppResponse() { }
        public XivGearAppResponse(bool error) { Error = error; }
    }

    public class XivGearApp_Set
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("items")]
        public XivGearApp_SetItems? Items { get; set; }

        [JsonProperty("food")]
        public int? Food { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class XivGearApp_SetItems
    {
        [JsonProperty]
        public XivGearApp_Item? Weapon { get; set; }

        [JsonProperty]
        public XivGearApp_Item? OffHand { get; set; }


        [JsonProperty]
        public XivGearApp_Item? Head { get; set; }

        [JsonProperty]
        public XivGearApp_Item? Body { get; set; }

        [JsonProperty]
        public XivGearApp_Item? Hand { get; set; }

        [JsonProperty]
        public XivGearApp_Item? Legs { get; set; }

        [JsonProperty]
        public XivGearApp_Item? Feet { get; set; }


        [JsonProperty]
        public XivGearApp_Item? Ears { get; set; }

        [JsonProperty]
        public XivGearApp_Item? Neck { get; set; }

        [JsonProperty]
        public XivGearApp_Item? Wrist { get; set; }

        [JsonProperty]
        public XivGearApp_Item? RingLeft { get; set; }

        [JsonProperty]
        public XivGearApp_Item? RingRight { get; set; }
    }

    public class XivGearApp_Item
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("materia")]
        public XivGearApp_Materia[]? Materia { get; set; }
    }

    public class XivGearApp_Materia
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }
}
