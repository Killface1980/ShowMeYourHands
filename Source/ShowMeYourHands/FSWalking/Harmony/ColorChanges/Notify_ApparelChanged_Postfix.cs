﻿using FacialStuff;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ShowMeYourHands.FSWalking
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
    internal class Notify_ApparelChanged_Postfix
    {
        public static void Postfix(Pawn_ApparelTracker __instance)
        {
            Pawn pawn = __instance.pawn;
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