using System;
using Dalamud.Configuration;

namespace HoardFarm.Model;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    public int HoardModeSave;
    public int HoardFarmMode;
    public int StopAfter = 50;
    public int StopAfterMode = 1;
    public int OverallRuns;
    public int OverallFoundHoards;
    public int OverallTime;
    public bool ShowOverlay = true;
    public bool ParanoidMode;
    
    public bool DoRetainers;
    public int RetainerMode = 1;

    public void Save()
    {
        PluginInterface.SavePluginConfig(this);
    }
}
