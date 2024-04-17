using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using HoardFarm.Tasks.Base;

namespace HoardFarm.Tasks;

public class TeleportTask(uint aetherytId, uint targetTerritoryId) : BaseTask(15)
{
    private bool teleportUsed;

    public override unsafe bool? Run()
    {
        if (!teleportUsed && Player.Territory != targetTerritoryId 
                          && ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 7) == 0)
        {
            Telepo.Instance()->Teleport(aetherytId, 0);
            teleportUsed = true;
        }

        return Player.Territory == targetTerritoryId && Player.Interactable && NotBusy();
    }

}
