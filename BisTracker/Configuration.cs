using BisTracker.BiS.Models;
using Dalamud.Configuration;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;

namespace BisTracker;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public bool LockMiniMenuR = true;
    public bool PinMiniMenu = false;

    public bool ShowMateriaMeldingWindows = true;
    public bool HighlightBisMateriaInMateriaMelder = true;
    public bool ShowAugmentedMeldsForUnaugmentedPieces = true;
    public bool UseMateriaNameInsteadOfMateriaValue = false;

    public int GenericThrottleTime = 750;
    public int PauseTimeBetweenSteps = 750;
    public int AnimationPauseTime = 4500;

    [NonSerialized]
    public int AccessoryCost = 375;
    [NonSerialized]
    public int LeftSmallCost = 495;
    [NonSerialized]
    public int LeftBigCost = 825;
    [NonSerialized]
    public int WeeklyTomeCap  = 450;

    public List<JobBis>? SavedBis { get; set; }

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }

    public void SaveJobBis(JobBis jobBis)
    {
        if (SavedBis == null) SavedBis = new List<JobBis>();
        var existingBis = SavedBis.SingleOrDefault(x => x.Name == jobBis.Name);
        if (existingBis != null)
        {
            SavedBis.Remove(existingBis);
        }

        SavedBis.Add(jobBis);
        Save();
    }

    public static Configuration Load()
    {
        try
        {
            var contents = File.ReadAllText(Svc.PluginInterface.ConfigFile.FullName);
            var json = JObject.Parse(contents);
            var version = (int?)json["Version"] ?? 0;
            return json.ToObject<Configuration>() ?? new();
        }
        catch (Exception e)
        {
            Svc.Log.Error($"Failed to load config from {Svc.PluginInterface.ConfigFile.FullName}: {e}");
            return new();
        }
    }

    public void UpdateConfig()
    {
        if (Version == 1)
        {
            foreach(var jobBis in SavedBis)
            {
                if (jobBis.XivGearAppSetItems == null) continue; //Safety in case something goes wrong
                jobBis.CreateBisItemsFromXivGearAppSetItems(jobBis.XivGearAppSetItems, null);
                jobBis.SelectedXivGearAppSet = null;
                jobBis.XivGearAppSetItems = null;
            }
        }

        Version = 2;
        P.Config.Save();
    }
}
