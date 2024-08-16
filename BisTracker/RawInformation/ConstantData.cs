using ECommons;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.RawInformation
{
    internal class ConstantData
    {
        public static Dictionary<int, LevelStats>? LevelStats;
        public static Dictionary<string, uint>? MainStatIds;
        public static Dictionary<string, uint>? FakeMainStatIds;
        public static Dictionary<string, uint>? SubStatIds;
        public static Dictionary<string, uint>? DoHStatIds;
        public static Dictionary<string, uint>? DoLStatIds;

        public static List<string> TwoHandedJobs = new List<string>()
        {
            "GLA", "CNJ", "THM", 
            "PLD", "CRP", "WHM",
            "BLM", "BSM", "ARM", 
            "GSM", "LTW", "WVR", 
            "ALC", "CUL", "MIN",
            "BTN", "FSH"
        };

        public static void Init()
        {
            try
            {
                var filePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "RawInformation/Data/CharLevelData.json");
                var jsonData = File.ReadAllText(filePath);

                LevelStats = JsonConvert.DeserializeObject<Dictionary<int, LevelStats>>(jsonData);
            }
            catch (Exception e)
            {
                Svc.Log.Error($"Failed to load config from {Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "RawInformation/Data/CharLevelData.json")}: {e}");
            }

            MainStatIds = new Dictionary<string, uint>()
            {
                { "Strength", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "strength").Value.RowId ?? (uint) 0 },
                { "Dexterity", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "dexterity").Value.RowId ?? (uint) 0 },
                { "Vitality", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "vitality").Value.RowId ?? (uint)0 },
                { "Intelligence", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "intelligence").Value.RowId ?? (uint) 0 },
                { "Mind", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "mind").Value.RowId ?? (uint) 0 }
            };

            FakeMainStatIds = new Dictionary<string, uint>()
            {
                { "Determination", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "determination").Value.RowId ?? (uint)0 },
                { "Piety", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "piety").Value.RowId ?? (uint)0 }
            };

            SubStatIds = new Dictionary<string, uint>()
            {
                { "Critical Hit", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "critical hit").Value.RowId ?? (uint)0 },
                { "Direct Hit Rate", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "direct hit rate").Value.RowId ?? (uint)0 },
                { "Spell Speed", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "spell speed").Value.RowId ?? (uint)0 },
                { "Skill Speed", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "skill speed").Value.RowId ?? (uint)0 },
                { "Tenacity", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "tenacity").Value.RowId ?? (uint)0 },
            };

            DoHStatIds = new Dictionary<string, uint>()
            {
                { "Control", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "control").Value.RowId ?? (uint)0 },
                { "Craftsmanship", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "craftsmanship").Value.RowId ?? (uint)0 },
                { "CP", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "cp").Value.RowId ?? (uint)0 },
            };

            DoLStatIds = new Dictionary<string, uint>()
            {
                { "Gathering", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "gathering").Value.RowId ?? (uint)0 },
                { "Perception", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "perception").Value.RowId ?? (uint)0 },
                { "GP", LuminaSheets.BaseParamSheet?.FirstOrDefault(x => x.Value.Name.ExtractText().ToLower() == "gp").Value.RowId ?? (uint)0 },
            };
        }
    }

    internal class LevelStats
    {
        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("baseMainStat")]
        public int BaseMainStat { get; set; }

        [JsonProperty("baseSubStat")]
        public int BaseSubStat { get; set; }

        [JsonProperty("levelDiv")]
        public int LevelDiv { get; set; }

        [JsonProperty("hp")]
        public int Health { get; set; }
        
        [JsonProperty("cp")]
        public int CP { get; set; }

        [JsonProperty("gp")]
        public int GP { get; set; }

        [JsonProperty("hpScalar")]
        public Dictionary<string, decimal> HealthScalar { get; set; }

        [JsonProperty("mainStatPowerMod")]
        public Dictionary<string, decimal> MainStatPowerMod { get; set; }
    }
}
