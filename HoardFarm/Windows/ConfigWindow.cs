using System;
using System.Diagnostics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HoardFarm.Windows;

public class ConfigWindow() : Window("Hoard Farm Config", ImGuiWindowFlags.AlwaysAutoResize), IDisposable
{
    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Actually no really much to configure here.");
        ImGui.Text("Wanna support me? Buy me a coffee!");
        ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);

        if (ImGui.Button("Support on Ko-fi"))
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://ko-fi.com/jukkales",
                UseShellExecute = true
            });

        ImGui.PopStyleColor(3);
        ImGui.Spacing();
        if (ImGui.Button("Reset Statistics"))
        {
            Config.OverallRuns = 0;
            Config.OverallFoundHoards = 0;
            Config.OverallTime = 0;
            Config.Save();

        }
    }
}
