using System;
using System.Linq;
using System.Timers;
using AutoRetainerAPI.Configuration;
using ECommons.GameHelpers;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HoardFarm.IPC;
using HoardFarm.Tasks;
using CSFramework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace HoardFarm.Service;

public class RetainerService : IDisposable
{
    public bool Running {get; private set;}
    private bool finished;
    private readonly Timer updateTimer;
    private DateTime startedAt;

    public RetainerService()
    {
        RetainerApi.OnRetainerPostprocessStep += CheckRetainerPostProcess;
        updateTimer = new Timer();
        updateTimer.Elapsed += OnTimerUpdate;
        updateTimer.Interval = 1000;
        updateTimer.Enabled = false;
    }


    public void Dispose()
    {
        RetainerApi.OnRetainerPostprocessStep -= CheckRetainerPostProcess;
        updateTimer.Dispose();
    }

    public void StartProcess()
    {
        Running = true;
        finished = false;
        updateTimer.Enabled = true;
        startedAt = DateTime.Now;
    }
    
    public unsafe void FinishProcess()
    {
        PluginLog.Information("Retainer processing finished.");
        if (GetOpenBellBehavior() != OpenBellBehavior.Enable_AutoRetainer)
        {
            PluginLog.Information("Disabling AutoRetainer.");
            DisableAutoRetainer();
        }
        Running = false;
        updateTimer.Enabled = false;

        if (CheckRetainerListOpen())
        {
            TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon);
            addon->Close(true);
        }
    }
    
    
    private unsafe void OnTimerUpdate(object? sender, ElapsedEventArgs e)
    {
        if (DateTime.Now.Subtract(startedAt).TotalMinutes > 5)
        {
            PluginLog.Information("Retainer processing took too long. Stopping.");
            FinishProcess();
        }
        
        if (!NavmeshIPC.NavIsReady())
        {
            return;
        }

        if (!TaskManager.IsBusy && Running)
        {
            if (Player.Territory != LimsaMapId)
            {
                Enqueue(new TeleportTask(LimsaAetherytId, LimsaMapId), "Teleport to Limsa");
                return;
            }

            if (ObjectTable.TryGetFirst(gameObject => gameObject.DataId == RetainerBellDataId, out var bell))
            {
                if (bell.Position.Distance(Player.GameObject->Position) > 3)
                {
                    Enqueue(new PathfindTask(RetainerBellLocation, true, 1f), "Move to bell");
                    EnqueueWait(2000); // SE server are just slow
                    return;
                }
            }

            if (NotBusy() || TargetSystem.Instance()->Target == null || TargetSystem.Instance()->Target->DataID != RetainerBellDataId)
            {
                Enqueue(new InteractObjectTask(RetainerBellDataId), "Interact Bell");
                if (GetOpenBellBehavior() != OpenBellBehavior.Enable_AutoRetainer)
                {
                    Enqueue(() => { 
                        EnableAutoRetainer();
                        return true;
                    });
                }
                return;
            }
            
            if (CheckRetainerListOpen() && finished)
            {
                FinishProcess();
            }
        }
    }

    private unsafe bool CheckRetainerListOpen()
    {
        return TryGetAddonByName<AtkUnitBase>("RetainerList", out var addon) && IsAddonReady(addon);
    }
    
    private OpenBellBehavior? GetOpenBellBehavior()
    {
        OpenBellBehavior? behavior = null;
        if (DalamudReflector.TryGetDalamudPlugin("AutoRetainer", out var plugin, false, true))
        {
            Safe(delegate
            {
                var type = plugin.GetType().Assembly.GetType("AutoRetainer.AutoRetainer", true);
                var result = type?.GetField("config", ReflectionHelper.AllFlags)?.GetValue(plugin);
                behavior = (OpenBellBehavior) result.GetFoP("OpenBellBehaviorWithVentures");
            });
        }

        return behavior;
    }

    private void EnableAutoRetainer()
    {
        if (DalamudReflector.TryGetDalamudPlugin("AutoRetainer", out var plugin, false, true))
        {
            Safe(delegate
            {
                var type = plugin.GetType().Assembly.GetType("AutoRetainer.Scheduler.SchedulerMain", true);
                var method = type?.GetMethod("EnablePlugin", ReflectionHelper.AllFlags);
                method?.Invoke(plugin, [PluginEnableReason.Manual]);
            });
        }
    }
    
    private void DisableAutoRetainer()
    {
        if (DalamudReflector.TryGetDalamudPlugin("AutoRetainer", out var plugin, false, true))
        {
            Safe(delegate
            {
                var type = plugin.GetType().Assembly.GetType("AutoRetainer.Scheduler.SchedulerMain", true);
                var method = type?.GetMethod("DisablePlugin", ReflectionHelper.AllFlags);
                method?.Invoke(plugin, null);
            });
        }
    }

    private void CheckRetainerPostProcess(string retainername)
    {
        PluginLog.Information($"Retainer {retainername} has finished processing.");
        if (CheckAllRetainersOnVenture())
        {
            PluginLog.Information("All retainers are processed.");
            finished = true;
        }
    }
    
    public static bool CheckRetainersDone(bool all = true)
    {
        var data = RetainerApi.GetOfflineCharacterData(ClientState.LocalContentId).RetainerData;
        return all ? data.All(e => CheckIsDone(e.VentureEndsAt)) : data.Any(e => CheckIsDone(e.VentureEndsAt));
    }
    
    public static bool CheckAllRetainersOnVenture()
    {
        var data = RetainerApi.GetOfflineCharacterData(ClientState.LocalContentId).RetainerData;
        return data.All(e => CheckIsDone(e.VentureEndsAt) == false);
    }
    
    private static bool CheckIsDone(long time)
    {
        return time+10 <= CSFramework.GetServerTime();
    }
}
