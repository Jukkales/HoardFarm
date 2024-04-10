using System;
using System.Numerics;
using System.Timers;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using HoardFarm.Model;
using HoardFarm.Tasks;
using Lumina.Excel.GeneratedSheets;

namespace HoardFarm.Service;

public class HoardFarmService : IDisposable
{
    public string HoardModeStatus = "";
    private bool hoardModeActive;
    private readonly Timer updateTimer;
    public int SessionRuns;
    public int SessionFoundHoards;
    public int SessionTime;

    private bool hoardFound;
    private bool hoardAvailable;
    private bool intuitionUsed;
    private bool movingToHoard;
    private Vector3 hoardPosition = Vector3.Zero;
    // private Collection<uint> chestIds = [];
    // private Collection<uint> visitedChestIds = [];
    private readonly string hoardFoundMessage;
    private readonly string senseHoardMessage;
    private readonly string noHoardMessage;

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
        
        Config.OverallRuns += SessionRuns;
        Config.OverallFoundHoards += SessionFoundHoards;
        Config.OverallTime += SessionTime;
        Config.Save();
        
        Reset();
    }

    private void EnableFarm()
    {
        Reset();
        SessionTime = 0;
        SessionRuns = 0;
        SessionFoundHoards = 0;
        updateTimer.Enabled = true;
        HoardModeStatus = "Running";
        ChatGui.ChatMessage += OnChatMessage;
    }

    private void Reset()
    {
        intuitionUsed = false;
        hoardFound = false;
        hoardPosition = Vector3.Zero;
        movingToHoard = false;
        hoardAvailable = false;
    }

    private void OnTimerUpdate(object? sender, ElapsedEventArgs e)
    {
        if (!TaskManager.IsBusy && hoardModeActive)
        {
            if (CheckDone())
            {
                if (InHoH)
                {
                    HoardModeStatus = "Ending";
                    TaskManager.Abort();
                    EnqueueImmediate(new LeaveDutyTask());
                }
                EnqueueImmediate(() =>
                {
                    HoardMode = false;
                    return true;
                });
                return;
            }
            if (!InHoH && !InRubySea && NotBusy() && !KyuseiInteractable())
            {
                HoardModeStatus = "Moving to HoH";
                Enqueue(new MoveToHoHTask());
            }
            
            if (InRubySea && NotBusy() && KyuseiInteractable())
            {
                HoardModeStatus = "Entering HoH";
                Enqueue(new EnterHeavenOnHigh());
            }

            if (InHoH && NotBusy())
            {
                if (!intuitionUsed)
                {
                    Enqueue(new UsePomanderTask(Pomander.Intuition));
                    intuitionUsed = true;
                }
                else
                {
                    if (hoardAvailable)
                    {
                        if (hoardPosition == Vector3.Zero && 
                            ObjectTable.TryGetFirst(gameObject => gameObject.DataId == 2007542, out var hoard))
                        {
                            hoardPosition = hoard.Position;
                        }
                        
                        if (hoardPosition != Vector3.Zero)
                        {
                            if (!movingToHoard)
                            {
                                Enqueue(new UsePomanderTask(Pomander.Concealment));
                                Enqueue(new PathfindTask(hoardPosition, true, 1.5f), 60 * 1000);
                                movingToHoard = true;
                                HoardModeStatus = "Move to Hoard";
                            }
                        }
                        else
                        {
                            if (!hoardFound)
                            {
                                HoardModeStatus = "Unreachable";
                                Enqueue(new LeaveDutyTask());
                                Enqueue(() =>
                                {
                                    SessionRuns++;
                                    return true;
                                });
                            }
                        }

                        if (hoardFound)
                        {
                            HoardModeStatus = "Leaving";
                            Enqueue(new LeaveDutyTask());
                            Enqueue(() =>
                            {
                                SessionRuns++;
                                SessionFoundHoards++;
                                return true;
                            });
                        }
                    }
                    else
                    {
                        HoardModeStatus = "Leaving";
                        Enqueue(new LeaveDutyTask());
                        Enqueue(() =>
                        {
                            SessionRuns++;
                            return true;
                        });
                    }
                }
            }
        }

        SessionTime++;
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
        XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
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
            HoardModeStatus = "No Hoard";
        }
        
        if (hoardFoundMessage.Equals(message.TextValue))
        {
            hoardFound = true;
            HoardModeStatus = "Done";
        }
    }
    
}
