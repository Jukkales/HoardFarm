using System;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using HoardFarm.IPC;
using HoardFarm.Model;
using ImGuiNET;

namespace HoardFarm.Windows;

public class MainWindow() : Window("Hoard Farm", ImGuiWindowFlags.AlwaysAutoResize), IDisposable
{
    private readonly Configuration conf = Config;

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGuiEx.AddHeaderIcon("OpenConfig", FontAwesomeIcon.Cog, new ImGuiEx.HeaderIconOptions() { Tooltip = "Open Config" }))
        {
            P.ShowConfigWindow();
        }

        using (_ = ImRaii.Disabled(!PluginInstalled(NavmeshIPC.Name)))
        {
            var enabled = HoardService.HoardMode;
            if (ImGui.Checkbox("Enable Hoard Farm Mode", ref enabled))
            {
                HoardService.HoardMode = enabled;
            }
        }
        if (!PluginInstalled(NavmeshIPC.Name))
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip($"This features requires {NavmeshIPC.Name} to be installed.");

        ImGui.SameLine(230);
        ImGui.Text(HoardService.HoardModeStatus);
        ImGui.Text("Stop After:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        var stopAfter = Config.StopAfter;
        if (ImGui.InputInt("##stopAfter", ref stopAfter))
        {
            Config.StopAfter = stopAfter;
            Config.Save();
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        
        var stopAfterMode = Config.StopAfterMode;
        if (ImGui.Combo("##stopAfterMode", ref stopAfterMode, new[] { "Runs", "Found Hoards", "Minutes" }, 3))
        {
            Config.StopAfterMode = stopAfterMode;
            Config.Save();
        }
        ImGui.Separator();
        
        ImGui.Text("Savegame:");
        ImGui.Indent(15);
        var save = conf.HoardModeSave;
        if (ImGui.RadioButton("Savegame 1", ref save, 0))
        {
            conf.HoardModeSave = save;
            Config.Save();
        }
        if(ImGui.RadioButton("Savegame 2", ref save, 1))
        {
            conf.HoardModeSave = save;
            Config.Save();
        }
        ImGui.Unindent(15);
        ImGui.Separator();
        ImGui.Text("Statistics:");
        
        ImGui.BeginGroup();
        
        ImGui.Text("Current Session");
        ImGui.Text("Runs: " + HoardService.SessionRuns);
        var sessionPercent = HoardService.SessionFoundHoards == 0 ? 0 : HoardService.SessionFoundHoards / (double)HoardService.SessionRuns * 100;
        ImGui.Text(
            $"Found: {HoardService.SessionFoundHoards}   ({sessionPercent:0.##} %%)");
        
        var sessionTimeAverage = HoardService.SessionFoundHoards == 0 ? 0 : HoardService.SessionTime / HoardService.SessionFoundHoards;
        if (sessionTimeAverage > 0)
        {
            ImGui.Text($"Time: {FormatTime(HoardService.SessionTime)}   (Ø {FormatTime(sessionTimeAverage, false)})");
        }
        else
        {
            ImGui.Text("Time: " + FormatTime(HoardService.SessionTime));
        }
        
        ImGui.EndGroup();
        ImGui.SameLine(170);
        ImGui.BeginGroup();
        
        ImGui.Text("Overall");
        ImGui.Text("Runs: " + Config.OverallRuns);
        var overallPercent = Config.OverallRuns == 0 ? 0 : Config.OverallFoundHoards / (double)Config.OverallRuns * 100;
        ImGui.Text(
            $"Found: {Config.OverallFoundHoards}   ({overallPercent:0.##} %%)");
        
        var overallTimeAverage = Config.OverallFoundHoards == 0 ? 0 : Config.OverallTime / Config.OverallFoundHoards;
        if (overallTimeAverage > 0)
        {
            ImGui.Text($"Time: {FormatTime(Config.OverallTime)}   (Ø {FormatTime(overallTimeAverage, false)})");
        }
        else
        {
            ImGui.Text("Time: " + FormatTime(Config.OverallTime));
        }
        
        ImGui.EndGroup();
        
    }
    
    private static String FormatTime(int seconds, bool withHours = true)
    {
        return TimeSpan.FromSeconds(seconds).ToString(withHours ? @"hh\:mm\:ss" : @"mm\:ss");
    }
}
