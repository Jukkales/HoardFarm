using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Timers;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using HoardFarm.IPC;
using HoardFarm.Model;
using HoardFarm.Tasks;
using HoardFarm.Tasks.TaskGroups;
using Lumina.Excel.GeneratedSheets;

namespace HoardFarm.Service;

public class HoardFarmService : IDisposable
{
    private record MapObject(uint ObjectId, uint DataId, Vector3 Position);
    
    public string HoardModeStatus = "";
    public string HoardModeError = "";
    private bool hoardModeActive;
    private readonly Timer updateTimer;
    public int SessionRuns;
    public int SessionFoundHoards;
    public int SessionTime;
    public bool FinishRun;

    private bool hoardFound;
    private bool hoardAvailable;
    private bool intuitionUsed;
    private bool movingToHoard;
    private bool searchMode;
    private bool safetyUsed;
    private Vector3 hoardPosition = Vector3.Zero;
    private readonly List<uint> visitedObjectIds = [];
    private readonly string hoardFoundMessage;
    private readonly string senseHoardMessage;
    private readonly string noHoardMessage;
    private readonly Dictionary<uint, MapObject> objectPositions = new();
    private DateTime runStarted;

    public HoardFarmService()
    {
        updateTimer = new Timer();
        updateTimer.Elapsed += OnTimerUpdate;
        updateTimer.Interval = 1000;
        updateTimer.Enabled = false;
        
        hoardFoundMessage = DataManager.GetExcelSheet<LogMessage>()!.GetRow(7274)!.Text.ToDalamudString().ExtractText();
        senseHoardMessage = DataManager.GetExcelSheet<LogMessage>()!.GetRow(7272)!.Text.ToDalamudString().ExtractText();
        noHoardMessage = DataManager.GetExcelSheet<LogMessage>()!.GetRow(7273)!.Text.ToDalamudString().ExtractText();
        
        ClientState.TerritoryChanged += OnMapChange;
    }
    
    public bool HoardMode
    {
        get => hoardModeActive;
        set
        {
            hoardModeActive = value;
            if (hoardModeActive)
            {
                EnableFarm();
            }
            else
            {
                DisableFarm();
            }
        }
    }

    private void DisableFarm()
    {
        updateTimer.Enabled = false;
        TaskManager.Abort();
        HoardModeStatus = "";
        ChatGui.ChatMessage -= OnChatMessage;
        Reset();

        if (RetainerScv.Running)
        {
            RetainerScv.FinishProcess();
        }
    }

    private void EnableFarm()
    {
        Reset();
        SessionTime = 0;
        SessionRuns = 0;
        SessionFoundHoards = 0;
        updateTimer.Enabled = true;
        HoardModeStatus = "Running";
        HoardModeError = "";
        ChatGui.ChatMessage += OnChatMessage;
    }

    private void Reset()
    {
        Config.Save();
        intuitionUsed = false;
        hoardFound = false;
        hoardPosition = Vector3.Zero;
        movingToHoard = false;
        hoardAvailable = false;
        searchMode = false;
        FinishRun = false;
        safetyUsed = false;
        objectPositions.Clear();
        runStarted = DateTime.Now;
    }

    private unsafe bool SearchLogic()
    {
        HoardModeStatus = "Searching";

        if (!TaskManager.IsBusy) {
            if (!objectPositions.Where(e => !visitedObjectIds.Contains(e.Value.ObjectId))
                                .Where(e => ChestIDs.Contains(e.Value.DataId))
                                .OrderBy(e => e.Value.Position.Distance(Player.Position))
                                .Select(e => e.Value)
                                .TryGetFirst(out var next))
            {
                if (!objectPositions.Where(e => !visitedObjectIds.Contains(e.Value.ObjectId))
                                    .OrderBy(e => e.Value.Position.Distance(Player.Position))
                                    .Select(e => e.Value)
                                    .TryGetFirst(out next))
                {
                    // We should never reach here normally .. but "never" is still a chance > 0% ;)
                    LeaveDuty("Unreachable");
                    return true;
                }
            }
            
            visitedObjectIds.Add(next!.ObjectId);
            Enqueue(new PathfindTask(next.Position, true), 60 * 1000, "Searching " + next.ObjectId);
        }

        FindHoardPosition();
        if (hoardPosition != Vector3.Zero)
        {
            NavmeshIPC.PathStop();
            TaskManager.Abort();
            return true;
        }

        return false;
    }

    private void OnTimerUpdate(object? sender, ElapsedEventArgs e)
    {
        // Retainer do not increase runtime
        if (RetainerScv.Running)
        {
            HoardModeStatus = "Retainer Running";
            return;
        }

        SessionTime++;
        Config.OverallTime++;        
        
        if (!NavmeshIPC.NavIsReady())
        {
            HoardModeStatus = "Waiting Navmesh";
            return;
        }      

        UpdateObjectPositions();
        SafetyChecks();
        
        if (searchMode && hoardPosition == Vector3.Zero)
            if (!SearchLogic())
                return;
        
        if (!TaskManager.IsBusy && hoardModeActive)
        {
            if (CheckDone() && !FinishRun)
            {
                FinishRun = true;
                return;
            }
            if (Player.Territory == HoHMapId1)
            {
                Error("Please prepare before starting.\nFloor One is not supported.");
                return;
            }
            if (!InHoH && !InRubySea && NotBusy() && !KyuseiInteractable())
            {
                HoardModeStatus = "Moving to HoH";
                Enqueue(new MoveToHoHTask());
                EnqueueWait(1000);
            }
            
            if (InRubySea && NotBusy() && KyuseiInteractable())
            {
                if (FinishRun)
                {
                    HoardModeStatus = "Finished";
                    HoardMode = false;
                    return; 
                }

                if (CheckRetainer())
                {
                    // Do retainers first
                    return;
                }
                
                HoardModeStatus = "Entering HoH";
                if (Config.ParanoidMode)
                {
                    EnqueueWait(Random.Shared.Next(3000, 6000));
                }
                Enqueue(new EnterHeavenOnHigh());
            }

            if (InHoH && NotBusy())
            {
                if (!intuitionUsed)
                {
                    if (!CheckMinimalSetup())
                    {
                        Error(
                            "Please prepare before starting.\nYou need at least one Intuition Pomander\nand one Concealment.");
                        return;
                    }
                    
                    if (CanUsePomander(Pomander.Intuition))
                    {
                        Enqueue(new UsePomanderTask(Pomander.Intuition), "Use Intuition");
                        intuitionUsed = true;
                    }
                }
                else
                {
                    if (hoardAvailable)
                    {
                        FindHoardPosition();
                        
                        if (hoardPosition != Vector3.Zero)
                        {
                            if (!movingToHoard)
                            {
                                // if (!Concealment)
                                // {
                                //     Enqueue(new UsePomanderTask(Pomander.Concealment, false), "Use Concealment");
                                // }
                                Enqueue(new PathfindTask(hoardPosition, true, 1.5f), 60 * 1000, "Move to Hoard");
                                movingToHoard = true;
                                HoardModeStatus = "Move to Hoard";
                            }
                        }
                        else
                        {
                            if (Config.HoardFarmMode == 1)
                            {
                                LeaveDuty();
                                return;
                            }
                            if (!hoardFound)
                            {
                                Enqueue(new UsePomanderTask(Pomander.Concealment), "Use Concealment");
                                searchMode = true;
                                return;
                            }
                        }

                        if (hoardFound)
                        {
                            LeaveDuty();
                        }
                    }
                    else
                    {
                        LeaveDuty();
                    }
                }
            }
        }
    }

    private void SafetyChecks()
    {
        if (InHoH && intuitionUsed)
        {
            if (DateTime.Now.Subtract(runStarted).TotalSeconds > 130 && !Svc.Condition[ConditionFlag.InCombat])
            {
                TaskManager.Abort();
                NavmeshIPC.PathStop();
                LeaveDuty("Timeout");
                return;
            }
            
            if (IsMoving())
            {
                if (!Concealment)
                {
                   if (CanUsePomander(Pomander.Concealment)) 
                   {
                       if (EzThrottler.Check("Concealment"))
                       {
                           EzThrottler.Throttle("Concealment", 2000);
                           new UsePomanderTask(Pomander.Concealment, false).Run();
                           return; // start next iteration
                       }
                   } else if (CanUsePomander(Pomander.Safety) && !safetyUsed && EzThrottler.Check("Concealment"))
                   {
                       if (EzThrottler.Check("Safety"))
                       {
                           EzThrottler.Throttle("Safety", 2000);
                           new UsePomanderTask(Pomander.Safety, false).Run();
                           safetyUsed = true;
                           return; // start next iteration
                       }
                   }
                }
            }

            if (Svc.Condition[ConditionFlag.InCombat])
            {
                if (CanUseMagicite() && EzThrottler.Check("Magicite"))
                {
                    EzThrottler.Throttle("Magicite", 6000);
                    new UseMagiciteTask().Run();
                }
            }
            
            if (Svc.Condition[ConditionFlag.Unconscious])
            {
                LeaveDuty("Player died");
            }
        }
    }

    private void UpdateObjectPositions()
    {
        foreach (var gameObject in ObjectTable)
            objectPositions.TryAdd(gameObject.EntityId, new MapObject(gameObject.EntityId, gameObject.DataId, gameObject.Position));
    }

    private bool CheckRetainer()
    {
        if (Config.DoRetainers 
            && RetainerService.CheckRetainersDone(Config.RetainerMode == 1) 
            && RetainerScv.CanRunRetainer())
        {
            RetainerScv.StartProcess();
            return true;
        }

        return false;
    }

    private bool CheckMinimalSetup()
    {
        if (!CanUsePomander(Pomander.Intuition))
        {
            return false;
        }
        if (CanUsePomander(Pomander.Concealment))
        {
            return true;
        }

        return CanUsePomander(Pomander.Safety) && CanUseMagicite();
    }

    private void LeaveDuty(string message = "Leaving")
    {
        HoardModeStatus = message;
        SessionRuns++;
        Config.OverallRuns++;
        Enqueue(new LeaveDutyTask());
    }

    private void Error(string message)
    {
        HoardModeStatus = "Error";
        HoardModeError = message;
        FinishRun = true;
        Enqueue(new LeaveDutyTask());
    }

    private void FindHoardPosition()
    {
        if (hoardPosition == Vector3.Zero && 
            ObjectTable.TryGetFirst(gameObject => gameObject.DataId == AccursedHoardId, out var hoard))
        {
            hoardPosition = hoard.Position;
        }
    }

    private bool CheckDone()
    {
        switch (Config.StopAfterMode)
        {
            case 0 when SessionRuns >= Config.StopAfter:
            case 1 when SessionFoundHoards >= Config.StopAfter:
            case 2 when SessionTime >= Config.StopAfter * 60:
                return true;
            default:
                return false;
        }
    }

    public void Dispose()
    {
        updateTimer.Dispose();
        ChatGui.ChatMessage -= OnChatMessage;
        ClientState.TerritoryChanged -= OnMapChange;
    }

    private void OnMapChange(ushort territoryType)
    {
        if (territoryType is HoHMapId11 or HoHMapId21)
        {
            Reset();
            HoardModeStatus = "Waiting";
        }
    }

    private void OnChatMessage(
        XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if (senseHoardMessage.Equals(message.TextValue))
        {
            intuitionUsed = true;
            hoardAvailable = true;
            HoardModeStatus = "Hoard Found";
        }
        
        if (noHoardMessage.Equals(message.TextValue))
        {
            intuitionUsed = true;
            hoardAvailable = false;
            TaskManager.Abort();
            LeaveDuty("No Hoard");
        }
        
        if (hoardFoundMessage.Equals(message.TextValue))
        {
            hoardFound = true;
            SessionFoundHoards++;
            Config.OverallFoundHoards++;
            Achievements.Progress++;
            TaskManager.Abort();
            LeaveDuty("Done");
        }
    }
    
}
