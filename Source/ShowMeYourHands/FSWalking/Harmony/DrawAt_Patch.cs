using System.Collections.Generic;
using FacialStuff;
using FacialStuff.Tweener;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowMeYourHands.FSWalking;

[ShowMeYourHandsMod.HotSwappable]
[HarmonyPriority(0)]
[HarmonyPatch(typeof(Pawn_DrawTracker), "DrawAt")]
class DrawAt_Patch
{

    [NotNull] public static Dictionary<Pawn,CompBodyAnimator> animatorDict = new ();

    static void Prefix(Pawn_DrawTracker __instance, ref Vector3 loc, Pawn ___pawn, out Vector3 __state)
    {
        __state = Vector3.zero;
        if (___pawn == null)
        {
            return;
        }
        if (___pawn.Dead)
        {
            return;
        }

        if (!animatorDict.ContainsKey(___pawn))
        {
            animatorDict[___pawn] = ___pawn.GetCompAnim();
        }

        CompBodyAnimator animator = animatorDict[___pawn];


        if (animator == null)
        {
            return;
        }
        //CompFace compFace = pawn.GetCompFace();
        if (___pawn.GetPosture() != PawnPosture.Standing)
        {
            return;
        };

        loc.x += animator?.BodyAnim?.offCenterX ?? 0f;
        __state = loc;
        animator?.ApplyBodyWobble(ref loc, ref __state);
     
        if (!Find.TickManager.Paused)
        {
            float rateMultiplier = Find.TickManager.TickRateMultiplier;
            float elapsedTime = 1f * rateMultiplier;

            foreach (Vector3Tween tween in animator.Vector3Tweens)
            {
                if (tween is { State: TweenState.Running })
                {
                    tween.Update(elapsedTime);
                }
            }
            foreach (FloatTween tween in animator.FloatTweens)
            {
                if (tween is { State: TweenState.Running })
                {
                    tween.Update(elapsedTime);
                }
            }


            /*
            if (angleTween.State == TweenState.Running)
            {
                compAnim.AimAngleTween.Update(3f * rateMultiplier);
            }
            */

        }

    }

    static void Postfix(Pawn_DrawTracker __instance, Vector3 loc, Vector3 __state, Pawn ___pawn)
    {
        Pawn pawn = ___pawn;

        if (__state == Vector3.zero)
        {
            return;
        }

        if (___pawn == null)
        {
            return;
        }
        if (pawn.Dead)
        {
            return;
        }

        if (!animatorDict.ContainsKey(___pawn))
        {
            animatorDict[___pawn] = ___pawn.GetCompAnim();
        }

        CompBodyAnimator animator = animatorDict[___pawn];


        if (animator == null)
        {
            return;

        }

        float bodyAngle = __instance.renderer.BodyAngle();
        // adding the pdd angle offset. could be a bug, but looks ok
        float handAngle = bodyAngle - animator.Offset_Angle;

        bool isStanding = pawn.GetPosture() == PawnPosture.Standing;

        float bodysizeScaling = animator.GetBodysizeScaling();
        
        if (bodysizeScaling < 1f)
        {
            float diffi = Mathf.Abs(1f - bodysizeScaling) / 3;
            __state.z -= diffi;
            loc.z -= diffi * 1.6f;
        }
        
        // add the offset to the hand as its tied to the body
        loc += animator.Offset_Pos;

        //keep the feet on the ground and steady. rotation and pos offset only in bed
        if (!isStanding)
        {
            __state += animator.Offset_Pos;
        }

        // Log.ErrorOnce("Scaled size: " + pawn + " - " + bodysizeScaling + " - " + loc + " - " + pos, Mathf.FloorToInt(bodysizeScaling * 100));


        float footAngle = isStanding ? 0f : handAngle;


        // do the tweening now
        /*if (compAnim.BodyAnim.bipedWithHands)
        {
            BodyAnimator.AnimatorTick();
        }
        */




        animator.CheckMovement();

        // feet shouldn't rotate while standing. 
        if (ShowMeYourHandsMod.instance.Settings.UseFeet)
        {
            animator?.DrawFeet(footAngle, __state, loc);
        }
        if (ShowMeYourHandsMod.instance.Settings.UsePaws )
        {
            animator?.DrawAnimalFeet(footAngle, __state, loc);
        }
        if (ShowMeYourHandsMod.instance.Settings.UseHands && (!ShowMeYourHandsMod.instance.Settings.ShowWhenCarry ||  pawn.carryTracker?.CarriedThing == null))
        {
            animator?.DrawHands(handAngle, loc);
        }
#pragma warning restore CS0162
    }

}