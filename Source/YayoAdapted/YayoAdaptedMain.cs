using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public static class YayoAdaptedMain
{
    static YayoAdaptedMain()
    {
        HarmonyLib.Harmony harmony = new("Mlie.ShowMeYourHands.YayoAdaptedCompatibility");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}
[AttributeUsage(AttributeTargets.Class)]
public class HotSwappableAttribute : Attribute
{
}