using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public static class DualWieldMain
{
    static DualWieldMain()
    {
        Harmony harmony = new("Mlie.ShowMeYourHands.DualWieldCompatibility");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}