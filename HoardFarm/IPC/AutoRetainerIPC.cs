using System;
using System.Diagnostics.CodeAnalysis;
using ECommons.EzIpcManager;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace HoardFarm.IPC;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnassignedReadonlyField")]
public class AutoRetainerIPC
{
    public AutoRetainerIPC() => EzIPC.Init(this, "AutoRetainer.PluginState");
    
    [EzIPC] public readonly Func<bool> IsBusy;
    [EzIPC] public readonly Func<int> GetInventoryFreeSlotCount;
}
