using System;
using System.Diagnostics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using HoardFarm.IPC;
using HoardFarm.Model;
using ImGuiNET;
using static HoardFarm.ImGuiEx.ImGuiEx;

namespace HoardFarm.Windows;

public class MainWindow() : Window($"Hoard Farm {P.GetType().Assembly.GetName().Version}###HoardFarm", ImGuiWindowFlags.AlwaysAutoResize)
{
    private readonly Configuration conf = Config;
    
    public override void Draw()
    {
        HeaderIcons();

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
        
        ImGui.BeginGroup();
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
        ImGui.EndGroup();
        
        ImGui.SameLine(170);
        
        ImGui.BeginGroup();
        ImGui.Text("Farm Mode:");
        ImGui.Indent(15);
        var farmMode = conf.HoardFarmMode;
        if (ImGui.RadioButton("Efficiency", ref farmMode, 0))
        {
            conf.HoardFarmMode = farmMode;
            Config.Save();
        }
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(FontAwesomeIcon.QuestionCircle.ToIconString());
        ImGui.PopFont();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Efficiency mode will search and try to find every hoard.\n" +
                             ">20%% of the hoards are unreachable from start.\n" +
                             "It will take some seconds to find it.\n" +
                             "This mode is still recommended.");
        }
        
        if(ImGui.RadioButton("Time", ref farmMode, 1))
        {
            conf.HoardFarmMode = farmMode;
            Config.Save();
        }
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(FontAwesomeIcon.QuestionCircle.ToIconString());
        ImGui.PopFont();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Time mode will NOT search for hoards.\n" +
                             "It instantly leaves if the hoard is unreachable. \n" +
                             "Faster runs but you will miss a lot of hoards (and a bit safer)");
        }
        ImGui.Unindent(15);
        ImGui.EndGroup();
        
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
        ImGui.Separator();
        
        ImGui.Text("Progress: " + Achievements.Progress + " / 20000");
        if (Achievements.Progress == 0)
        {
            ImGui.Text($"Damn it will take ages. Trust me");
        } else if (overallTimeAverage == 0)
        {
            ImGui.Text($"Suffer more. Unable to calculate the remaining time.");
        } else {
            ImGui.Text(
                $"You will need at least {FormatRemaining((20000 - Achievements.Progress) * overallTimeAverage)}\nof farming to complete the achievement.");
        }

        if (HoardService.HoardModeError != string.Empty) {
            ImGui.Separator();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1),"Unable to run:\n");
            ImGui.Text(HoardService.HoardModeError);
        }
        
    }

    private static void HeaderIcons()
    {
        if (AddHeaderIcon("OpenConfig", FontAwesomeIcon.Cog, new HeaderIconOptions { Tooltip = "Open Config" }))
        {
            P.ShowConfigWindow();
        }
        if (AddHeaderIcon("OpenHelp", FontAwesomeIcon.QuestionCircle, new HeaderIconOptions { Tooltip = "Open Help" }))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Jukkales/HoardFarm/wiki/How-to-run",
                UseShellExecute = true
            });
        }
        if (AddHeaderIcon("KofiLink", FontAwesomeIcon.Heart, new HeaderIconOptions { Tooltip = "Support me ♥",  Color = 0xFF3030D0 }))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://ko-fi.com/jukkales",
                UseShellExecute = true
            });
        }
    }
    
    private static String FormatTime(int seconds, bool withHours = true)
    {
        return TimeSpan.FromSeconds(seconds).ToString(withHours ? @"hh\:mm\:ss" : @"mm\:ss");
    }
    
    private static String FormatRemaining(int seconds)
    {
        var timespan = TimeSpan.FromSeconds(seconds);
        return (timespan.Days >= 1 ? timespan.Days + " Days and " : "") + timespan.ToString(@"hh\:mm\:ss");
    }
}
