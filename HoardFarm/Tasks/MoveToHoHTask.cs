using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace HoardFarm.Tasks;

public class MoveToHoHTask : IBaseTask
{
    public bool? Run()
    {
        Enqueue(() => TeleportToOnokoro(), 15 * 1000, "Onokoro Teleport");
        Enqueue(() => WaitTillOnokoro(), 15 * 1000, "Wait for Onokoro");
        Enqueue(new MountTask());
        Enqueue(new PathfindTask(KyuseiLocation), 60 * 1000, "Move to Kyusei");
        return true;
    }

    private static unsafe bool TeleportToOnokoro()
    {
        if (WaitTillOnokoro()) return true;

        if (ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 7) == 0)
        {
            Telepo.Instance()->Teleport(TeleportAetherytId, 0);
            return true;
        }

        return false;
    }
}
