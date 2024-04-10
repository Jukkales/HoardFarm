using System.Collections.Generic;
using System.Numerics;

namespace HoardFarm.Utils;

public static class DataIDs
{
    public const uint KyuseiDataId = 1025846;
    public static readonly Vector3 KyuseiLocation = new(-2.02948f, 3.0059814f, -611.3528f);
    
    public const ushort RubySeaMapId = 613;
    public const ushort HoHMapId11 = 771;
    public const ushort HoHMapId21 = 772;
    
    public const uint TeleportAetherytId = 106;
    
    public const uint AbandonDutyMessageId = 2545;
    public const uint ConfirmPartyKoMessageId = 10483;
    
    public const uint VanishStatusId = 1496;
    public const uint AccursedHoardId = 2007542;
    
    public static readonly HashSet<uint> ChestIDs = [2007357, 2007358];

}
