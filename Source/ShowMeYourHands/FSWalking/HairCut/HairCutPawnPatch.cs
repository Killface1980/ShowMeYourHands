using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnAnimator.HairCut;

[HarmonyPatch(typeof(PawnGraphicSet), nameof(PawnGraphicSet.HairMatAt))]
public class HairCutPawnPatch
{
    public static void Postfix(PawnGraphicSet __instance, Rot4 facing, bool portrait, bool cached, ref Material __result)
    {
        if (!PawnAnimatorMod.instance.Settings.CutHair)
        {
            return;
        }

        Pawn pawn = __instance.pawn;
        HairCutPawn hairPawn = CutHairDB.GetHairCache(pawn);
        Graphic graphic = hairPawn.HairCutGraphic;

        if (graphic == null)
        {
            return;
            // Class3.ResolveApparelGraphics_Postfix(this.Pawn.Drawer.renderer.graphics);

        }

        Material material = graphic?.MatAt(facing);
        if (!portrait && pawn.IsInvisible())
        {
            material = InvisibilityMatPool.GetInvisibleMat(material);
        }

        if (!material.NullOrBad() && !cached)
        {
            material = __instance.flasher.GetDamagedMat(material);
        }

        if (!material.NullOrBad())
        {
            __result = material;
        }
    }

}