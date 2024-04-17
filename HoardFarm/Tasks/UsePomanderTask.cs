using ECommons.Automation;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HoardFarm.Model;
using HoardFarm.Tasks.Base;

namespace HoardFarm.Tasks;

public class UsePomanderTask(Pomander pomander, bool wait = true) : BaseTask
{
    public override unsafe bool? Run()
    {
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonStatus", out var addon) && IsAddonReady(addon))
        {
            if (CanUsePomander(pomander))
            {
                Callback.Fire(addon, true, 11, (int)pomander);
                if(wait)
                    EnqueueWaitImmediate(2000);
            }
            return true;
        }

        if (EzThrottler.Throttle("OpenDeepDungeonStatus", 2000)) {
            AgentDeepDungeonStatus.Instance()->AgentInterface.Show();
        }

        return false;
    }
}
