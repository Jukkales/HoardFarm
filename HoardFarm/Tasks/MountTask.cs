using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using HoardFarm.IPC;

namespace HoardFarm.Tasks;

public class MountTask : IBaseTask
{
    public unsafe bool? Run()
    {
        if (Svc.Condition[ConditionFlag.Mounted] && NotBusy()) return true;
        if (!Svc.Condition[ConditionFlag.Casting] && !Svc.Condition[ConditionFlag.Unknown57])
        {
            ActionManager.Instance()->UseAction(ActionType.GeneralAction, 24);
        }

        return false;
    }
}
