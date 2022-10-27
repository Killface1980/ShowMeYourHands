using System;
using System.Reflection;
using Verse;

namespace ShowMeYourHandsYayoAdapted;

[StaticConstructorOnStartup]
public static class YayoAdaptedMain
{
    static YayoAdaptedMain()
    {
        HarmonyLib.Harmony harmony = new("Killface.PawnAnimator.YayoAdaptedCompatibility");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
[AttributeUsage(AttributeTargets.Class)]
public class HotSwappableAttribute : Attribute
{
}