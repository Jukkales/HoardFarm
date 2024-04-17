using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using HoardFarm.Tasks.Base;

namespace HoardFarm.Tasks;

public class InteractObjectTask(uint dataId) : BaseTask()
{
    public override unsafe bool? Run()
    {
        if (ObjectTable.TryGetFirst(e => e.DataId == dataId, out var obj))
        {
            if (TargetSystem.Instance()->Target == (GameObject*)obj.Address)
            {
                TargetSystem.Instance()->InteractWithObject((GameObject*)obj.Address);
                return true;
            }

            if (EzThrottler.Throttle("Interact" + dataId))
            {
                TargetSystem.Instance()->Target = (GameObject*)obj.Address;
                return false;
            }
        }

        return false;
    }
}
