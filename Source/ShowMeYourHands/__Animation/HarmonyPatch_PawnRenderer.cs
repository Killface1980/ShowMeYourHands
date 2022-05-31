using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using FacialStuff;
using ShowMeYourHands;

namespace Rimworld_Animations
{
    [ShowMeYourHandsMod.HotSwappableAttribute]
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal", new Type[]
		{
					typeof(Vector3),
					typeof(float),
					typeof(bool),
					typeof(Rot4),
					typeof(RotDrawMode),
					typeof(PawnRenderFlags)
		}
	)]
	public static class HarmonyPatch_PawnRenderer
	{

		[HarmonyBefore(new string[] { "showhair.kv.rw", "erdelf.HumanoidAlienRaces", "Nals.FacialAnimation" })]
		public static void Prefix(PawnRenderer __instance, ref Vector3 rootLoc, ref float angle, bool renderBody, ref Rot4 bodyFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
        {
			if (flags.FlagSet(PawnRenderFlags.Portrait)) return;

			PawnGraphicSet graphics = __instance.graphics;
			Pawn pawn = graphics.pawn;
			CompBodyAnimator bodyAnim = pawn.TryGetComp<CompBodyAnimator>();


			if (bodyAnim is { isAnimating: true } && pawn.Map == Find.CurrentMap)
			{
				bodyAnim.animatePawnBody(ref rootLoc, ref angle, ref bodyFacing);

			}

		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> ins = instructions.ToList();

			for (int i = 0; i < instructions.Count(); i++)
			{

				if (i - 3 >= 0 && ins[i - 3].opcode == OpCodes.Call && ins[i - 3].operand != null && ins[i - 3].OperandIs(AccessTools.DeclaredMethod(typeof(PawnRenderer), "BaseHeadOffsetAt")))
				{

					yield return new CodeInstruction(OpCodes.Ldloca, (object)0);
					yield return new CodeInstruction(OpCodes.Ldloca, (object)7);
					yield return new CodeInstruction(OpCodes.Ldloca, (object)6);
					yield return new CodeInstruction(OpCodes.Ldarga, (object)2);
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(PawnRenderer), "pawn"));
					yield return new CodeInstruction(OpCodes.Ldarg, (object)6);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(AnimationUtility), "AdjustHead"));
					yield return ins[i];
					//headFacing equals true
				}

				else
				{
					yield return ins[i];
				}


			}

		}

	}



}
