using System.Collections;
using HoardFarm.Tasks.Base;

namespace HoardFarm.Tasks.TaskGroups;

public class MoveToHoHTask : IBaseTaskGroup
{
    public ArrayList GetTaskList()
    {
        return [
            new TeleportTask(OnokoroAetherytId, RubySeaMapId),
            new MountTask(),
            new PathfindTask(KyuseiLocation)
        ];
    }
}
