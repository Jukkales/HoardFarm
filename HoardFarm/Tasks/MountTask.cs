using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using HoardFarm.Tasks.Base;

namespace HoardFarm.Tasks;

public class MountTask : BaseTask
{
    public override unsafe bool? Run()
    {
        if (Svc.Condition[ConditionFlag.Mounted] && NotBusy()) return true;
        if (!Svc.Condition[ConditionFlag.Casting] && !Svc.Condition[ConditionFlag.MountOrOrnamentTransition])
        {
            ActionManager.Instance()->UseAction(ActionType.GeneralAction, 24);
        }

        return false;
    }
}
