using System.Numerics;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace HoardFarm.Windows;

public class DeepDungeonMenuOverlay : Window
{
    public DeepDungeonMenuOverlay() : base("DeepDungeonMenuOverlay",
                                           ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        RespectCloseHotkey = false;
        IsOpen = true;
    }
    
    public override unsafe bool DrawConditions()
    {
        if (!InRubySea || !KyuseiInteractable() || !Config.ShowOverlay) return false;
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonMenu", out var menu) && IsAddonReady(menu))
        {
            var width = menu->GetRootNode()->Width * menu->GetRootNode()->GetScaleX();
            Position = new Vector2(menu->X + width - 125, menu->Y - 40);
            return true;
        }
        
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonSaveData", out var save) && IsAddonReady(save))
        {
            var width = save->GetRootNode()->Width * save->GetRootNode()->GetScaleX();
            Position = new Vector2(save->X + width - 125, save->Y - 40);
            return true;
        }
        return false;
    }
    
    public override void Draw()
    {
        if (ImGui.Button("Open Hoardfarm"))
        {
            P.ShowMainWindow();
        }
    }
}
