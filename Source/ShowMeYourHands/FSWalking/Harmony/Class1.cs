using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ShowMeYourHands.FSWalking.Harmony
{
    [ShowMeYourHandsMod.HotSwappable]
    [HarmonyPatch(typeof(PawnRenderer), "CarryWeaponOpenly")]
    public static class PawnRenderer_CarryWeaponOpenly_Postfix
    {
        [HarmonyPostfix]
        public static void CarryWeaponOpenly(ref PawnRenderer __instance, ref Pawn ___pawn, ref bool __result)
        {
            return;
            if (__result) return;

            SkillDef skillDef = ___pawn.CurJob?.RecipeDef?.workSkill;
            JobDriver curDriver = ___pawn.jobs?.curDriver;

            if (curDriver?.ActiveSkill != null)
            {
                skillDef = curDriver.ActiveSkill;
            }
            if (skillDef != null)
            {
                if (HasReleventStatModifiers(___pawn?.equipment?.Primary, skillDef))
                {
                    __result = true;
                }
            }
        }

        public static bool HasReleventStatModifiers(Thing weapon, SkillDef skill)
        {
            if (weapon == null)
            {
                return false;
            }
            List<StatModifier> equippedStatOffsets = weapon.def.equippedStatOffsets;
            if (skill != null && equippedStatOffsets != null)
            {
                foreach (StatModifier item in equippedStatOffsets)
                {
                    List<SkillNeed> skillNeedOffsets = item.stat.skillNeedOffsets;
                    List<SkillNeed> skillNeedFactors = item.stat.skillNeedFactors;
                    if (skillNeedOffsets != null)
                    {
                        foreach (SkillNeed item2 in skillNeedOffsets)
                        {
                            if (skill == item2.skill)
                            {
                                return true;
                            }
                        }
                    }
                    if (skillNeedFactors == null)
                    {
                        continue;
                    }
                    foreach (SkillNeed item3 in skillNeedFactors)
                    {
                        if (skill == item3.skill)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}