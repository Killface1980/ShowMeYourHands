﻿using FacialStuff;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ShowMeYourHands.FSWalking
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Apparel), nameof(Apparel.Notify_ColorChanged))]
    internal class Notify_ColorChanged_Postfix
    {
        public static void Postfix(Apparel __instance)
        {
            Pawn pawn = __instance.Wearer;
            if (pawn == null)
            {
                return;
            }

            LongEventHandler.ExecuteWhenFinished(
                () =>
                {
                    pawn.GetCompAnim()?.ClearCache();
                    pawn.GetCompAnim()?.pawnBodyGraphic?.Initialize();
                });

        }
    }
}