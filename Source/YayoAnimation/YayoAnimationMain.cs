using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowMeYourHands;

[StaticConstructorOnStartup]
public static class YayoAnimationMain
{
    static YayoAnimationMain()
    {
        HarmonyLib.Harmony harmony = new("Mlie.ShowMeYourHands.YayoAnimationCompatibility");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

    }
}
[AttributeUsage(AttributeTargets.Class)]
public class HotSwappableAttribute : Attribute
{
}