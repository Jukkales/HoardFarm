using System;
using System.Collections;
using System.Runtime.InteropServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HoardFarm.Tasks.Base;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;
#pragma warning disable CS8974 // Converting method group to non-delegate type

namespace HoardFarm.Tasks.TaskGroups;

public class LeaveDutyTask : IBaseTaskGroup
{
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct EventObject {
        [FieldOffset(0)] public ulong Unknown0;
        [FieldOffset(8)] public ulong Unknown8;
    }
    
    public ArrayList GetTaskList()
    {
        return
        [
            LeaveDuty,
            new SelectYesnoTask(AbandonDutyMessageId),
            WaitReady
        ];
    }
    
    private static unsafe bool? LeaveDuty()
    {
        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.ContentsFinderMenu);
        if (agent == null)
        {
            return false;
        }
        
        var eventObject = stackalloc EventObject[1];
        var atkValues = CreateEventParams();
        if (atkValues != null)
        {
            try {
                agent->ReceiveEvent(eventObject, atkValues, 1, 0);
                return true;
            } finally {
                Marshal.FreeHGlobal(new IntPtr(atkValues));
            }
        }

        return false;
    }
    
    public static unsafe AtkValue* CreateEventParams() {
        try {
            var atkValues = (AtkValue*)Marshal.AllocHGlobal(sizeof(AtkValue));
            if (atkValues == null) return null;
            atkValues[0].Type = ValueType.Int;
            atkValues[0].Int = 0;
            return atkValues;
        } catch (Exception) {
            return null;
        }
    }
    
    private static unsafe bool? WaitReady()
    {
        if (!InRubySea || !Player.Interactable || !NotBusy()) return false;
        
        if (ObjectTable.TryGetFirst(e => e.DataId == KyuseiDataId, out var npc))
        {
            if (TargetSystem.Instance()->Target == (GameObject*)npc.Address)
            {
                return true;
            }

            if (EzThrottler.Throttle("InteractKyusei"))
            {
                TargetSystem.Instance()->Target = (GameObject*)npc.Address;
                return false;
            }
        }

        return false;
    }


}
