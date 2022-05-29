using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using rjw;

namespace Rimworld_Animations {

    [HarmonyPatch(typeof(JobDriver_SexBaseInitiator), "Start")]
    static class HarmonyPatch_JobDriver_SexBaseInitiator_Start {
        public static void Postfix(ref JobDriver_SexBaseInitiator __instance) {
			/*
			 These particular jobs need special code
			 don't play anim for now
			 */
			if(__instance is JobDriver_Masturbate || __instance is JobDriver_ViolateCorpse) {
				return;
			}

			if(!AnimationSettings.PlayAnimForNonsexualActs && NonSexualAct(__instance))
            {
				return;
            }

			Pawn pawn = __instance.pawn;

			Building_Bed bed = __instance.Bed;

			if ((__instance.Target as Pawn)?.jobs?.curDriver is JobDriver_SexBaseReciever) {

				Pawn Target = __instance.Target as Pawn;

				bool quickie = (__instance is JobDriver_SexQuick) && AnimationSettings.fastAnimForQuickie;

				int preAnimDuration = __instance.duration;
				int AnimationTimeTicks = 0;


				if (bed != null) {
                    RerollAnimations(Target, out AnimationTimeTicks, bed as Thing, __instance.Sexprops.sexType, quickie, sexProps: __instance.Sexprops);
                }
				else {
					RerollAnimations(Target, out AnimationTimeTicks, sexType: __instance.Sexprops.sexType, fastAnimForQuickie: quickie, sexProps: __instance.Sexprops);
				}


				//Modify Orgasm ticks to only orgasm as many times as RJW stock orgasm allows
				if(AnimationTimeTicks != 0)
                {
					__instance.orgasmstick = preAnimDuration * __instance.orgasmstick / AnimationTimeTicks;
				}
				

			}
		}

		public static void RerollAnimations(Pawn pawn, out int AnimationTimeTicks, Thing bed = null, xxx.rjwSextype sexType = xxx.rjwSextype.None, bool fastAnimForQuickie = false, rjw.SexProps sexProps = null) {

			AnimationTimeTicks = 0;

			if(pawn == null || !(pawn.jobs?.curDriver is JobDriver_SexBaseReciever)) {
				Log.Error("Error: Tried to reroll animations when pawn isn't sexing");
				return;
			}

			List<Pawn> pawnsToAnimate = (pawn.jobs.curDriver as JobDriver_SexBaseReciever).parteners.ToList();

			if (!pawnsToAnimate.Contains(pawn)) {
				pawnsToAnimate = pawnsToAnimate.Append(pawn).ToList();
			}

			for(int i = 0; i < pawnsToAnimate.Count; i++) {

				if(pawnsToAnimate[i].TryGetComp<CompBodyAnimator>() == null) {
					Log.Error("Error: " + pawnsToAnimate[i].Name + " of race " + pawnsToAnimate[i].def.defName + " does not have CompBodyAnimator attached!");
					break;
				}
			}

			AnimationDef anim = AnimationUtility.tryFindAnimation(ref pawnsToAnimate, sexType, sexProps);

			if (anim != null) {

				bool mirror = GenTicks.TicksGame % 2 == 0;

				IntVec3 pos = pawn.Position;
				
				for (int i = 0; i < anim.actors.Count; i++)
				{
					pawnsToAnimate[i].TryGetComp<CompBodyAnimator>().isAnimating = false;
				}

				for (int i = 0; i < pawnsToAnimate.Count; i++) {

					if (bed != null)
						pawnsToAnimate[i].TryGetComp<CompBodyAnimator>().setAnchor(bed);
					else {

						pawnsToAnimate[i].TryGetComp<CompBodyAnimator>().setAnchor(pos);
					}

					bool shiver = pawnsToAnimate[i].jobs.curDriver is JobDriver_SexBaseRecieverRaped;
					pawnsToAnimate[i].TryGetComp<CompBodyAnimator>().StartAnimation(anim, pawnsToAnimate, i, mirror, shiver, fastAnimForQuickie);

					int animTicks = anim.animationTimeTicks - (fastAnimForQuickie ? anim.animationStages[0].playTimeTicks : 0);
					(pawnsToAnimate[i].jobs.curDriver as JobDriver_Sex).ticks_left = animTicks;
					(pawnsToAnimate[i].jobs.curDriver as JobDriver_Sex).sex_ticks = animTicks;
					(pawnsToAnimate[i].jobs.curDriver as JobDriver_Sex).duration = animTicks;


					AnimationTimeTicks = animTicks;

					if(!AnimationSettings.hearts) {
						(pawnsToAnimate[i].jobs.curDriver as JobDriver_Sex).ticks_between_hearts = Int32.MaxValue;
					}

				} 
			}
			else {
				Log.Message("No animation found");

				/*

				//if pawn isn't already animating,
				if (!pawn.TryGetComp<CompBodyAnimator>().isAnimating) {
					(pawn.jobs.curDriver as JobDriver_SexBaseReciever).increase_time(duration);
					//they'll just do the thrusting anim
				}

				*/
			}
		}


		static IEnumerable<String> NonSexActRulePackDefNames = new String[]
		{
			"MutualHandholdingRP",
			"MutualMakeoutRP",
		};

		public static bool NonSexualAct(JobDriver_SexBaseInitiator sexBaseInitiator)
        {
			if(NonSexActRulePackDefNames.Contains(sexBaseInitiator.Sexprops.rulePack))
            {
				return true;
            }
			return false;
        }
	}

	[HarmonyPatch(typeof(JobDriver_SexBaseInitiator), "End")]
	static class HarmonyPatch_JobDriver_SexBaseInitiator_End {

		public static void Postfix(ref JobDriver_SexBaseInitiator __instance) {

			if ((__instance.Target as Pawn)?.jobs?.curDriver is JobDriver_SexBaseReciever) {
				if (__instance.pawn.TryGetComp<CompBodyAnimator>().isAnimating) {

					List<Pawn> parteners = ((__instance.Target as Pawn)?.jobs.curDriver as JobDriver_SexBaseReciever).parteners;

					for (int i = 0; i < parteners.Count; i++) {

						//prevents pawns who started a new anim from stopping their new anim
						if (!((parteners[i].jobs.curDriver as JobDriver_SexBaseInitiator) != null && (parteners[i].jobs.curDriver as JobDriver_SexBaseInitiator).Target != __instance.pawn))
							parteners[i].TryGetComp<CompBodyAnimator>().isAnimating = false;

					}

					__instance.Target.TryGetComp<CompBodyAnimator>().isAnimating = false;

					if (xxx.is_human((__instance.Target as Pawn))) {
						(__instance.Target as Pawn)?.Drawer.renderer.graphics.ResolveApparelGraphics();
						PortraitsCache.SetDirty((__instance.Target as Pawn));
					}
				}

				((__instance.Target as Pawn)?.jobs.curDriver as JobDriver_SexBaseReciever).parteners.Remove(__instance.pawn);

			}

			if (xxx.is_human(__instance.pawn)) {
				__instance.pawn.Drawer.renderer.graphics.ResolveApparelGraphics();
				PortraitsCache.SetDirty(__instance.pawn);
			}
		}
	}
}
