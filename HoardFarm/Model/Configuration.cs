﻿using System;
using Dalamud.Configuration;

namespace HoardFarm.Model;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    public int HoardModeSave { get; set; }
    public int StopAfter { get; set; } = 50;
    public int StopAfterMode { get; set; } = 1;
    public int OverallRuns { get; set; }
    public int OverallFoundHoards { get; set; }
    public int OverallTime { get; set; }

    public void Save()
    {
        PluginInterface.SavePluginConfig(this);
    }
}