using ECommons.Automation;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HoardFarm.Tasks;

public class EnterHeavenOnHigh : IBaseTask
{
    public bool? Run()
    {
        Enqueue(WaitForYesAlreadyDisabledTask, "Disable Yes Already");
        Enqueue(InteractKyusei, "Interact Kyusei");
        Enqueue(EnterDuty, "Enter Heaven on High");
        Enqueue(SelectSave, "Select Save");
        Enqueue(new SelectYesnoTask(ConfirmPartyKoMessageId), "Confirm Party KO");
        Enqueue(ConfirmDuty, "Confirm Duty");
        Enqueue(WaitTillDutyReady, "Wait Till Duty Ready");
        return true;
    }
    
     private static unsafe bool? InteractKyusei()
    {
        if (ObjectTable.TryGetFirst(e => e.DataId == KyuseiDataId, out var npc))
        {
            if (TargetSystem.Instance()->Target == (GameObject*)npc.Address)
            {
                TargetSystem.Instance()->InteractWithObject((GameObject*)npc.Address);
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
    
    private static unsafe bool? EnterDuty()
    {
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonMenu", out var menu) && IsAddonReady(menu))
        {
            Callback.Fire(menu, true, 0);
            return true;
        }

        return false;
    }
    
    private static unsafe bool? SelectSave()
    {
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonSaveData", out var menu) && IsAddonReady(menu))
        {
            Callback.Fire(menu, true, Config.HoardModeSave);
            return true;
        }

        return false;
    }
    
    private static unsafe bool? ConfirmDuty()
    {
        if (TryGetAddonByName<AtkUnitBase>("ContentsFinderConfirm", out var menu) && IsAddonReady(menu))
        {
            Callback.Fire(menu, true, 8);
            return true;
        }

        return false;
    }
    
    private static bool? WaitTillDutyReady()
    {
        if (InHoH && Player.Interactable && NotBusy())
        {
            EnqueueWaitImmediate(1000);
            return true;
        }

        return false;
    }
}
