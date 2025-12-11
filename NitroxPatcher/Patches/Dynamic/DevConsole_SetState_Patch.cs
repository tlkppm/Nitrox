using System.Reflection;
using NitroxClient.GameLogic;
using NitroxModel.Helper;

namespace NitroxPatcher.Patches.Dynamic;

public sealed partial class DevConsole_SetState_Patch : NitroxPatch, IDynamicPatch
{
    public static readonly MethodInfo TARGET_METHOD = Reflect.Method((DevConsole t) => t.SetState(default(bool)));

    public static bool Prefix(bool value)
    {
        if (value && NitroxConsole.DisableConsole)
        {
            return false;
        }
        return true;
    }
}
