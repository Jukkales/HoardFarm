using ECommons.Automation;
using ECommons.Throttlers;

namespace HoardFarm.Tasks;

public class SelectYesnoTask(uint messageId) : IBaseTask
{
    public unsafe bool? Run()
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
