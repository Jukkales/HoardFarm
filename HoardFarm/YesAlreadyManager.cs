using System.Collections.Generic;
using ECommons.DalamudServices;

namespace HoardFarm;

public static class YesAlreadyManager
{
    
    private static bool Reenable = false;
    private static HashSet<string>? Data = null;

    public static void GetData()
    {
        if (Data != null) return;
        if (Svc.PluginInterface.TryGetData<HashSet<string>>("YesAlready.StopRequests", out var data))
        {
            Data = data;
        }
    }

    public static void DisableIfNeeded()
    {
        GetData();
        if (Data != null)
        {
            PluginLog.Information("Disabling Yes Already (new)");
            Data.Add(Svc.PluginInterface.InternalName);
            Reenable = true;
        }
    }

    public static void EnableIfNeeded()
    {
        if (Reenable)
        {
            GetData();
            if (Data != null)
            {
                PluginLog.Information("Enabling Yes Already (new)");
                Data.Remove(Svc.PluginInterface.InternalName);
                Reenable = false;
            }
        }
    }

    public static bool IsEnabled()
    {
        GetData();
        if (Data != null)
        {
            return !Data.Contains(Svc.PluginInterface.InternalName);
        }
        return false;
    }

    public static void Tick()
    {
        if (TaskManager.IsBusy)
        {
            if (IsEnabled())
            {
                DisableIfNeeded();
            }
        }
        else
        {
            if (Reenable)
            {
                EnableIfNeeded();
            }
        }
    }

    public static bool? WaitForYesAlreadyDisabledTask()
    {
        return !IsEnabled();
    }
}
