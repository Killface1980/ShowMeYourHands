﻿using System.Collections.Generic;
using FacialStuff;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowMeYourHands.FSWalking
{
    [HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.BodyAngle))]
    class BodyAngle_Patch
    {
        static void Postfix(PawnRenderer __instance, ref float __result, Pawn ___pawn)
        {
            if (___pawn == null || !___pawn.GetCompAnim(out CompBodyAnimator compAnim))
            {
                return;
            }

            if (compAnim.IsMoving)
            {
                __result += compAnim.BodyAngle;
            }
        }

    }

    [HarmonyPatch(typeof(PawnRenderer), "DrawDynamicParts")]
    public class Patch_PawnRenderer_DrawDynamicParts
    {
        //static pawnDrawData pdd;
        [HarmonyPriority(0)]
        public static void Prefix(PawnRenderer __instance, ref Vector3 rootLoc, ref float angle, ref Rot4 pawnRotation, PawnRenderFlags flags, Pawn ___pawn)
        {
            if (___pawn == null || !___pawn.GetCompAnim(out CompBodyAnimator compAnim))
            {
                return;
            }

            if (compAnim.IsMoving)
            {
                angle += compAnim.BodyAngle;
            }
        }
    }

    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
    public class Patch_PawnRenderer_RenderPawnInternal
    {
        public static bool skipPatch;

        [HarmonyPriority(0)]
        public static void Prefix(PawnRenderer __instance, ref Vector3 rootLoc, ref float angle, bool renderBody, ref Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags, Pawn ___pawn)
        {
            if (skipPatch)
            {
                skipPatch = false;
                return;
            }
            if (___pawn == null || !___pawn.GetCompAnim(out CompBodyAnimator compAnim))
            {
                return;
            }

            if (compAnim.IsMoving)
            {
                angle += compAnim.BodyAngle;
            }
        }
    }
    [HarmonyBefore("com.yayo.yayoAni")]
    [HarmonyPatch(typeof(PawnRenderer), "RenderCache")]
    public class Patch_PawnRenderer_RenderCache
    {
        [HarmonyPriority(0)]
        public static void Prefix(PawnRenderer __instance, Pawn ___pawn, Dictionary<Apparel, (Color, bool)> ___tmpOriginalColors, Rot4 rotation, ref float angle, Vector3 positionOffset, bool renderHead, bool renderBody, bool portrait, bool renderHeadgear, bool renderClothes, Dictionary<Apparel, Color> overrideApparelColor = null, Color? overrideHairColor = null, bool stylingStation = false)
        {
            if (portrait)
            {
                Patch_PawnRenderer_RenderPawnInternal.skipPatch = true;
                return;
            }
            if (___pawn == null || !___pawn.GetCompAnim(out CompBodyAnimator compAnim))
            {
                return;
            }

            if (compAnim.IsMoving)
            {
                angle += compAnim.BodyAngle;
            }

            Patch_PawnRenderer_RenderPawnInternal.skipPatch = true;

        }
    }


}
