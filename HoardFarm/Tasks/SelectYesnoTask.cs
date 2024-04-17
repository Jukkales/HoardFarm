using ECommons.Automation;
using ECommons.Throttlers;
using HoardFarm.Tasks.Base;

namespace HoardFarm.Tasks;

public class SelectYesnoTask(uint messageId) : BaseTask
{
    public override unsafe bool? Run()
    {
        var addon = FindSelectYesNo(messageId);
        if (addon != null && IsAddonReady(addon) && EzThrottler.Throttle("SelectYesnoTask" + messageId))
        {
            Callback.Fire(addon, true, 0);
            return true;
        }

        return false;
    }
}
