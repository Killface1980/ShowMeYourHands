using System.Reflection;
using Verse;

namespace ShowMeYourHandsDualWield;

[StaticConstructorOnStartup]
public static class DualWieldMain
{
    static DualWieldMain()
    {
        HarmonyLib.Harmony harmony = new("Killface.PawnAnimator.DualWieldCompatibility");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}