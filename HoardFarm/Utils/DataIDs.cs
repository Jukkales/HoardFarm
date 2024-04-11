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
    
    public const uint ForTheHoardAchievementId = 3184;
    
    public static readonly HashSet<uint> ChestIDs = 
        [1036, 1037, 1038, 1039, 1040, 1041, 1042, 1043, 1044, 1045, 1046, 1047, 1048, 1049, 2007357, 2007358];

}
