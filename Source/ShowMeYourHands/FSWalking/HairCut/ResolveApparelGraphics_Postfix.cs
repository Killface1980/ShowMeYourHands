using System.Collections.Generic;
using System.Linq;
using PawnAnimator.HairCut;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace PawnAnimator.FSWalking.HairCut
{
    [HarmonyPatch(typeof(PawnGraphicSet), nameof(PawnGraphicSet.ResolveApparelGraphics))]
    internal static class ResolveApparelGraphics_Postfix
    {
        public static void Postfix(PawnGraphicSet __instance)
        {
            if (!PawnAnimatorMod.instance.Settings.CutHair)
            {
                return;
            }

            Pawn pawn = __instance.pawn;

            // Set up the hair cut graphic
            if (true) // Controller.settings.MergeHair) //todo menu
            {
                HairCutPawn hairPawn = CutHairDB.GetHairCache(pawn);

                List<Apparel> wornApparel = pawn.apparel.WornApparel
                    .Where(x => x.def.apparel.LastLayer == ApparelLayerDefOf.Overhead).ToList();

                HeadCoverage coverage = HeadCoverage.FullHead;

                //ToDo: Deactivated for now, needs more refinement ---needs clarification 2020-07-27

                if (!wornApparel.NullOrEmpty())
                {
                    if (Enumerable.Any(wornApparel, x => x.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead)))
                    {
                        coverage = HeadCoverage.UpperHead;
                    }

                    if (Enumerable.Any(wornApparel, x => x.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead)))
                    {
                        coverage = HeadCoverage.FullHead;
                    }
                }

                if (coverage != 0)
                {
                    hairPawn.HairCutGraphic = CutHairDB.Get<Graphic_Multi>(
                        pawn.story.hairDef.texPath,
                        ShaderDatabase.Cutout,
                        Vector2.one,
                        pawn.story.hairColor, coverage);
                }
            }
        }

    }
}
