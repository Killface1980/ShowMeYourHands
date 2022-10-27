using System;
using System.Reflection;
using Verse;

namespace ShowMeYourHandsYayoAni;

[StaticConstructorOnStartup]
public static class YayoAnimationMain
{
    static YayoAnimationMain()
    {
        HarmonyLib.Harmony harmony = new("Killface.PawnAnimator.YayoAnimationCompatibility");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

    }
}
[AttributeUsage(AttributeTargets.Class)]
public class HotSwappableAttribute : Attribute
{
}