using System;
using ECommons.EzHookManager;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace HoardFarm.Service;

public unsafe class AchievementService : IDisposable
{
    
    public delegate void ReceiveAchievementProgressDelegate(Achievement* achievement, uint id, uint current, uint max);
    [EzHook("C7 81 ?? ?? ?? ?? ?? ?? ?? ?? 89 91 ?? ?? ?? ?? 44 89 81")]
    public EzHook<ReceiveAchievementProgressDelegate> ReceiveAchievementProgressHook = null!;

    public int Progress { get; set; }
    
    public AchievementService()
    {
        EzSignatureHelper.Initialize(this);
        ReceiveAchievementProgressHook.Enable();
    }
    
    public void UpdateProgress()
    {
        Achievement.Instance()->RequestAchievementProgress(ForTheHoardAchievementId);
    }
    
    private void ReceiveAchievementProgressDetour(Achievement* achievement, uint id, uint current, uint max)
    {
        if (id == ForTheHoardAchievementId)
        {
            PluginLog.Debug($"Achievement progress: {current}/{max}");
            Progress = (int)current;
        }
        ReceiveAchievementProgressHook.Original(achievement, id, current, max);
    }
    
    public void Dispose()
    {
    }
}
