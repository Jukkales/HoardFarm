using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HoardFarm.Model;
using Lumina.Excel.GeneratedSheets;

namespace HoardFarm.Utils;

public static class Utils
{
    public static readonly Version MinAutoRetainerVersion = new(4, 2, 6, 3);
    public static float SprintCD => Player.Status.FirstOrDefault(s => s.StatusId == 50)?.RemainingTime ?? 0;

    public static bool Concealment =>
        (Player.Status.FirstOrDefault(s => s.StatusId == VanishStatusId)?.RemainingTime ?? 0) > 0;

    public static bool InHoH => Player.Territory == HoHMapId11 || Player.Territory == HoHMapId21;
    public static bool InRubySea => Player.Territory == RubySeaMapId;

    public static unsafe bool IsMoving()
    {
        return AgentMap.Instance()->IsPlayerMoving == 1;
    }

    public static float Distance(this Vector3 v, Vector3 v2)
    {
        return new Vector2(v.X - v2.X, v.Z - v2.Z).Length();
    }

    public static bool PluginInstalled(string name)
    {
        return DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);
    }

    public static unsafe bool NotBusy()
    {
        var occupied = IsOccupied();
        var target = TargetSystem.Instance()->Target;

        if (occupied && Svc.Condition[ConditionFlag.OccupiedInQuestEvent] && target != null &&
            target->DataID == KyuseiDataId) occupied = false;

        return Player.Available
               && Player.Object.CastActionId == 0
               && !occupied
               && !Svc.Condition[ConditionFlag.Jumping]
               && Player.Object.IsTargetable;
    }


    public static unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params uint[] ids)
    {
        if (node == null || ids.Length <= 0) return null;

        if (node->NodeID == ids[0])
        {
            if (ids.Length == 1) return node;

            var newList = new List<uint>(ids);
            newList.RemoveAt(0);

            var childNode = node->ChildNode;
            if (childNode != null) return GetNodeByIDChain(childNode, newList.ToArray());

            if ((int)node->Type >= 1000)
            {
                var componentNode = node->GetAsAtkComponentNode();
                var component = componentNode->Component;
                var uldManager = component->UldManager;
                childNode = uldManager.NodeList[0];
                return childNode == null ? null : GetNodeByIDChain(childNode, newList.ToArray());
            }

            return null;
        }

        //check siblings
        var sibNode = node->PrevSiblingNode;
        return sibNode != null ? GetNodeByIDChain(sibNode, ids) : null;
    }

    public static unsafe AtkUnitBase* FindSelectYesNo(uint rowId)
    {
        var s = Svc.Data.GetExcelSheet<Addon>()!.GetRow(rowId)!.Text
                   .ToDalamudString().ExtractText();
        for (var i = 1; i < 100; i++)
            try
            {
                if (TryGetAddonByName<AtkUnitBase>("SelectYesno", out var addon) && IsAddonReady(addon))
                {
                    var textNode = addon->UldManager.NodeList[15]->GetAsAtkTextNode();
                    var text = MemoryHelper.ReadSeString(&textNode->NodeText).ExtractText().Replace(" ", "");
                    if (text.EqualsAny(s) || text.ContainsAny(s)) return addon;
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

        return null;
    }


    public static unsafe bool KyuseiInteractable()
    {
        if (ObjectTable.TryGetFirst(e => e.DataId == KyuseiDataId, out var npc))
            return npc.Position.Distance(Player.GameObject->Position) < 7f;
        return false;
    }

    public static unsafe bool CanUsePomander(Pomander pomander)
    {
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonStatus", out var addon) && IsAddonReady(addon))
        {
            var chain = pomander switch
            {
                Pomander.Intuition => IntuitionChain,
                Pomander.Concealment => ConcealmentChain,
                Pomander.Safety => SafetyChain,
                _ => []
            };
            return chain.Length != 0 && GetNodeByIDChain(addon->GetRootNode(), chain)->IsVisible;
        }

        return false;
    }

    public static unsafe bool CanUseMagicite()
    {
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonStatus", out var addon) && IsAddonReady(addon))
            return GetNodeByIDChain(addon->GetRootNode(), MagiciteChain)->IsVisible;

        return false;
    }

    public static bool AutoRetainerVersionHighEnough()
    {
        return Svc.PluginInterface.InstalledPlugins.FirstOrDefault(x => x.IsLoaded && x.InternalName == "AutoRetainer")
                  ?.Version >= MinAutoRetainerVersion;
    }
}
