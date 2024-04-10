using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HoardFarm.Utils;

public static class Utils
{
    public static float SprintCD => Player.Status.FirstOrDefault(s => s.StatusId == 50)?.RemainingTime ?? 0;
    public static bool Concealment => (Player.Status.FirstOrDefault(s => s.StatusId == VanishStatusId)?.RemainingTime ?? 0) > 0;
    public static bool InHoH => Player.Territory == HoHMapId11 || Player.Territory == HoHMapId21;
    public static bool InRubySea => Player.Territory == RubySeaMapId;
    public static unsafe bool IsMoving() => AgentMap.Instance()->IsPlayerMoving == 1;
    public static float Distance(this Vector3 v, Vector3 v2) => new Vector2(v.X - v2.X, v.Z - v2.Z).Length();
    public static bool PluginInstalled(string name) => DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);
    
    public static bool NotBusy()
    {
        return Player.Available
               && Player.Object.CastActionId == 0 
               && !IsOccupied() 
               && !Svc.Condition[ConditionFlag.Jumping] 
               && Player.Object.IsTargetable;
    }

    public static unsafe AtkUnitBase* FindSelectYesNo(uint rowId)
    {
        var s = Svc.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Addon>()!.GetRow(rowId)!.Text
                  .ToDalamudString().ExtractText();
        for (var i = 1; i < 100; i++)
        {
            try
            {
                if (TryGetAddonByName<AtkUnitBase>("SelectYesno", out var addon) && IsAddonReady(addon))
                {
                    var textNode = addon->UldManager.NodeList[15]->GetAsAtkTextNode();
                    var text = MemoryHelper.ReadSeString(&textNode->NodeText).ExtractText().Replace(" ", "");
                    if (text.EqualsAny(s) || text.ContainsAny(s))
                    {
                        return addon;
                    }
                    {
                        return addon;
                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.Debug(e.ToString());
                return null;
            }
        }
        return null;
    }
    
    
    public static unsafe bool KyuseiInteractable()
    {
        if (ObjectTable.TryGetFirst(e => e.DataId == KyuseiDataId, out var npc))
        {
            return npc.Position.Distance(Player.GameObject->Position) < 3f;
        }
        return false;
    }
    
    public static bool WaitTillOnokoro()
    {
        return InRubySea && Player.Interactable && NotBusy();
    }
}
