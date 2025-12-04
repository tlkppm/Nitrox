using System.ComponentModel;

namespace NitroxModel.Server;

/// <summary>
/// GameModes according to Subnautica's enum GameModeOption
/// </summary>
public enum NitroxGameMode
{
    [Description("生存模式")]
    SURVIVAL = 0,
    
    [Description("自由模式")]
    FREEDOM = 2,
    
    [Description("极限模式")]
    HARDCORE = 257,
    
    [Description("创造模式")]
    CREATIVE = 1790,
}
