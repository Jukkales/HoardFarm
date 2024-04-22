using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using HoardFarm.IPC;
using HoardFarm.Model;
using ImGuiNET;
using static HoardFarm.ImGuiEx.ImGuiEx;

namespace HoardFarm.Windows;

public class MainWindow() : Window($"Hoard Farm {P.GetType().Assembly.GetName().Version}###HoardFarm",
                                   ImGuiWindowFlags.AlwaysAutoResize)
{
    private readonly Configuration conf = Config;

    public override void Draw()
    {
        HeaderIcons();

        using (_ = ImRaii.Disabled(!PluginInstalled(NavmeshIPC.Name)))
        {
            var enabled = HoardService.HoardMode;
            if (ImGui.Checkbox("Enable Hoard Farm Mode", ref enabled)) HoardService.HoardMode = enabled;
        }

        if (!PluginInstalled(NavmeshIPC.Name))
        {
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip($"This features requires {NavmeshIPC.Name} to be installed.");
        }

        ImGui.SameLine(230);
        ImGui.Text(HoardService.HoardModeStatus);
        ImGui.Text("Stop After:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("##stopAfter", ref Config.StopAfter))
            Config.Save();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);

        if (ImGui.Combo("##stopAfterMode", ref Config.StopAfterMode, ["Runs", "Found Hoards", "Minutes"], 3))
            Config.Save();

        DrawRetainerSettings();

        ImGui.Separator();

        ImGui.BeginGroup();
        ImGui.Text("Savegame:");
        ImGui.Indent(15);
        
        if (ImGui.RadioButton("Savegame 1", ref conf.HoardModeSave, 0))
            Config.Save();

        if (ImGui.RadioButton("Savegame 2", ref conf.HoardModeSave, 1))
            Config.Save();

        ImGui.Unindent(15);
        ImGui.EndGroup();

        ImGui.SameLine(170);

        ImGui.BeginGroup();
        ImGui.Text("Farm Mode:");
        ImGui.Indent(15);
        if (ImGui.RadioButton("Efficiency", ref conf.HoardFarmMode, 0))
            Config.Save();

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

        if (ImGui.RadioButton("Safety", ref conf.HoardFarmMode, 1))
            Config.Save();
        

        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(FontAwesomeIcon.QuestionCircle.ToIconString());
        ImGui.PopFont();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Safety mode will NOT search for hoards.\n" +
                             "It instantly leaves if the hoard is unreachable. \n" +
                             "Safer and faster runs but you will miss a lot of hoards.\n" +
                             "It will take longer overall.");
        }

        ImGui.Unindent(15);
        ImGui.EndGroup();

        ImGui.Separator();
        ImGui.Text("Statistics:");

        ImGui.BeginGroup();

        ImGui.Text("Current Session");
        ImGui.Text("Runs: " + HoardService.SessionRuns);
        var sessionPercent = HoardService.SessionFoundHoards == 0
                                 ? 0
                                 : HoardService.SessionFoundHoards / (double)HoardService.SessionRuns * 100;
        ImGui.Text(
            $"Found: {HoardService.SessionFoundHoards}   ({sessionPercent:0.##} %%)");

        var sessionTimeAverage = HoardService.SessionFoundHoards == 0
                                     ? 0
                                     : HoardService.SessionTime / HoardService.SessionFoundHoards;
        if (sessionTimeAverage > 0)
            ImGui.Text($"Time: {FormatTime(HoardService.SessionTime)}   (Ø {FormatTime(sessionTimeAverage, false)})");
        else
            ImGui.Text("Time: " + FormatTime(HoardService.SessionTime));

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
            ImGui.Text($"Time: {FormatTime(Config.OverallTime)}   (Ø {FormatTime(overallTimeAverage, false)})");
        else
            ImGui.Text("Time: " + FormatTime(Config.OverallTime));

        ImGui.EndGroup();
        ImGui.Separator();

        ImGui.Text("Progress: " + Achievements.Progress + " / 20000");
        if (Achievements.Progress == 0)
            ImGui.Text("Damn it will take ages. Trust me");
        else if (overallTimeAverage == 0)
            ImGui.Text("Suffer more. Unable to calculate the remaining time.");
        else
        {
            ImGui.Text(
                $"You will need at least {FormatRemaining((20000 - Achievements.Progress) * overallTimeAverage)}\nof farming to complete the achievement.");
        }

        if (HoardService.HoardModeError != string.Empty)
        {
            ImGui.Separator();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(new Vector4(1, 0, 0, 1), FontAwesomeIcon.ExclamationTriangle.ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Unable to run:\n");
            ImGui.Text(HoardService.HoardModeError);
        }
    }

    private void DrawRetainerSettings()
    {
        var autoRetainer = RetainerApi.Ready && AutoRetainerVersionHighEnough();
        using (_ = ImRaii.Disabled(!autoRetainer))
        {
            if (ImGui.Checkbox("Do retainers:", ref Config.DoRetainers)) 
                Config.Save();
        }

        var hoverText = "Ports to Limsa Lominsa and runs retainers between runs if done.";
        if (!autoRetainer)
            hoverText = "This features requires AutoRetainer 4.2.6.3 or higher to be installed and configured.";

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) ImGui.SetTooltip(hoverText);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(170);
        if (ImGui.Combo("##retainerMode", ref Config.RetainerMode,
                        ["If ANY Retainer is done", "If ALL Retainer are done"], 2)) 
            Config.Save();
    }

    private static void HeaderIcons()
    {
        if (AddHeaderIcon("OpenConfig", FontAwesomeIcon.Cog, new HeaderIconOptions { Tooltip = "Open Config" }))
            P.ShowConfigWindow();
        
        if (AddHeaderIcon("OpenHelp", FontAwesomeIcon.QuestionCircle, new HeaderIconOptions { Tooltip = "Open Help" }))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Jukkales/HoardFarm/wiki/How-to-run",
                UseShellExecute = true
            });
        }

        if (AddHeaderIcon("KofiLink", FontAwesomeIcon.Heart,
                          new HeaderIconOptions { Tooltip = "Support me ♥", Color = 0xFF3030D0 }))
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
        var timespan = TimeSpan.FromSeconds(seconds);
        return timespan.ToString(withHours ? timespan.Days >= 1 ? @"d\:hh\:mm\:ss" : @"hh\:mm\:ss" : @"mm\:ss");
    }

    private static String FormatRemaining(int seconds)
    {
        var timespan = TimeSpan.FromSeconds(seconds);
        return (timespan.Days >= 1 ? timespan.Days + " Days and " : "") + timespan.ToString(@"hh\:mm\:ss");
    }
}
