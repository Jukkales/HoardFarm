using ECommons.Automation;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HoardFarm.Model;

namespace HoardFarm.Tasks;

public class UsePomanderTask(Pomander pomander) : IBaseTask
{
    public unsafe bool? Run()
    {
        if (TryGetAddonByName<AtkUnitBase>("DeepDungeonStatus", out var addon) && IsAddonReady(addon))
        {
            Callback.Fire(addon, true, 11, (int)pomander);
            EnqueueWaitImmediate(2000);
            return true;
        }

        if (EzThrottler.Throttle("OpenDeepDungeonStatus", 2000)) {
            AgentDeepDungeonStatus.Instance()->AgentInterface.Show();
        }

        return false;
    }
}
