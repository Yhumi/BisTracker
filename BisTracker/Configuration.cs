using BisTracker.BiS.Models;
using Dalamud.Configuration;
using Dalamud.Plugin;
using ECommons.DalamudServices;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BisTracker;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool LockMiniMenuR = true;
    public bool PinMiniMenu = false;

    public List<JobBis>? SavedBis { get; set; }

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Svc.PluginInterface.SavePluginConfig(this);
    }

    public void SaveJobBis(JobBis jobBis)
    {
        if (SavedBis == null) SavedBis = new List<JobBis>();
        var existingBis = SavedBis.SingleOrDefault(x => x.Job == jobBis.Job);
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
}
