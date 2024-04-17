using ECommons.Automation;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HoardFarm.Tasks.Base;

namespace HoardFarm.Tasks;

public class UseMagiciteTask() : BaseTask
{
    public override unsafe bool? Run()
    {
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonStatus", out var addon) && IsAddonReady(addon))
        {
            if (CanUseMagicite())
            {
                Callback.Fire(addon, true, 12, 0);
                EnqueueWaitImmediate(3000);
            }

            return true;
        }

        if (EzThrottler.Throttle("OpenDeepDungeonStatus", 2000)) {
            AgentDeepDungeonStatus.Instance()->AgentInterface.Show();
        }

        return false;
    }
    
}
