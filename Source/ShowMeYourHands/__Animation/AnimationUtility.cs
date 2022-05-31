using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FacialStuff;
using RimWorld;
using ShowMeYourHands;
/*using rjw.Modules.Interactions.Helpers;
using rjw.Modules.Interactions.Objects;
*/using UnityEngine;
using Verse;
using Verse.AI;
/*using rjw.Modules.Interactions.Enums;
*/
namespace Rimworld_Animations
{
    [ShowMeYourHandsMod.HotSwappable]
    public static class AnimationUtility
    {
        /*  
         Note: always make the list in this order:
             Female pawns, animal female pawns, male pawns, animal male pawns
        */
        public static AnimationDef tryFindAnimation(ref List<Pawn> participants/*, rjw.xxx.rjwSextype sexType = 0, rjw.SexProps sexProps = null*/)
        {


/*            InteractionWithExtension interaction = InteractionHelper.GetWithExtension(sexProps.dictionaryKey);


            if (interaction.HasInteractionTag(InteractionTag.Reverse))
            {
                Pawn buffer = participants[1];
                participants[1] = participants[0];
                participants[0] = buffer;
            }
*/
            participants =
                participants/*.OrderBy(p => p.jobs.curDriver is rjw.JobDriver_SexBaseInitiator)
                .OrderBy(p => rjw.xxx.can_fuck(p))*/
                .ToList();


            List<Pawn> localParticipants = new List<Pawn>(participants);

            bool Predicate(AnimationDef x)
            {
                /*
                if (x.actors.Count != localParticipants.Count)
                {
                    return false;
                }

                for (int i = 0; i < x.actors.Count; i++)
                {
                    if (rjw.RJWPreferenceSettings.Malesex == rjw.RJWPreferenceSettings.AllowedSex.Nohomo)
                    {
                        if (rjw.xxx.is_male(localParticipants[i]) && x.actors[i].isFucked)
                        {
                            return false;
                        }
                    }

                    if (x.actors[i].requiredGender != null && !x.actors[i].requiredGender.Contains(localParticipants[i].gender.ToStringSafe<Gender>()))
                    {
                        if (AnimationSettings.debugMode)
                        {
                            Log.Message(string.Concat(new string[] { x.defName.ToStringSafe<string>(), " not selected -- ", localParticipants[i].def.defName.ToStringSafe<string>(), " ", localParticipants[i].Name.ToStringSafe<Name>(), " does not match required gender" }));
                        }

                        return false;
                    }

                    if ((x.actors[i].blacklistedRaces != null) && x.actors[i].blacklistedRaces.Contains(localParticipants[i].def.defName))
                    {
                        if (AnimationSettings.debugMode) Log.Message(x.defName.ToStringSafe() + " not selected -- " + localParticipants[i].def.defName.ToStringSafe() + " " + localParticipants[i].Name.ToStringSafe() + " is blacklisted");
                        return false;
                    }

                    if (x.actors[i].defNames.Contains("Human"))
                    {
                        if (!rjw.xxx.is_human(localParticipants[i]))
                        {
                            if (AnimationSettings.debugMode) Log.Message(x.defName.ToStringSafe() + " not selected -- " + localParticipants[i].def.defName.ToStringSafe() + " " + localParticipants[i].Name.ToStringSafe() + " is not human");

                            return false;
                        }
                    }
                    else if (!x.actors[i].bodyDefTypes.Contains(localParticipants[i].RaceProps.body))
                    {
                        if (!x.actors[i].defNames.Contains(localParticipants[i].def.defName))
                        {
                            if (rjw.RJWSettings.DevMode)
                            {
                                string animInfo = x.defName.ToStringSafe() + " not selected -- " + localParticipants[i].def.defName.ToStringSafe() + " " + localParticipants[i].Name.ToStringSafe() + " is not ";
                                foreach (String defname in x.actors[i].defNames)
                                {
                                    animInfo += defname + ", ";
                                }

                                if (AnimationSettings.debugMode) Log.Message(animInfo);
                            }

                            return false;
                        }
                    }
                    //genitals checking

                    if (!GenitalCheckForPawn(x.actors[i].requiredGenitals, localParticipants[i], out string failReason))
                    {
                        Debug.Log("Didn't select " + x.defName + ", " + localParticipants[i].Name + " " + failReason);
                        return false;
                    }

                    //TESTING ANIMATIONS ONLY REMEMBER TO COMMENT OUT BEFORE PUSH
                    if (x.defName != "Cunnilingus") return false;


                    if (x.actors[i].isFucking && !rjw.xxx.can_fuck(localParticipants[i]))
                    {
                        if (AnimationSettings.debugMode) Log.Message(x.defName.ToStringSafe() + " not selected -- " + localParticipants[i].def.defName.ToStringSafe() + " " + localParticipants[i].Name.ToStringSafe() + " can't fuck");
                        return false;
                    }

                    if (x.actors[i].isFucked && !rjw.xxx.can_be_fucked(localParticipants[i]))
                    {
                        if (AnimationSettings.debugMode) Log.Message(x.defName.ToStringSafe() + " not selected -- " + localParticipants[i].def.defName.ToStringSafe() + " " + localParticipants[i].Name.ToStringSafe() + " can't be fucked");
                        return false;
                    }
                }
*/

                return true;
            }
                IEnumerable<AnimationDef> options = DefDatabase<AnimationDef>.AllDefs.Where(Predicate);
            List<AnimationDef> optionsWithInteractionType = options.ToList().FindAll(x => x.interactionDefTypes != null /*&& x.interactionDefTypes.Contains(sexProps.sexType.ToStringSafe())*/);
            if (optionsWithInteractionType.Any())
            {
                if (ShowMeYourHandsModSettings.debugMode)
                    Log.Message("Selecting animation for interaction type " + /*sexProps.sexType.ToStringSafe() +*/ "...");
                return optionsWithInteractionType.RandomElement();
            }
            List<AnimationDef> optionsWithSexType = options.ToList()/*.FindAll(x => x.sexTypes != null && x.sexTypes.Contains(sexType))*/;
            if (optionsWithSexType.Any())
            {
                if (ShowMeYourHandsModSettings.debugMode)
                    Log.Message("Selecting animation for rjwSexType " /*+ sexType.ToStringSafe()*/ + "...");
                return optionsWithSexType.RandomElement();
            }

/*            if(optionsWithInitiator.Any()) {
                if (AnimationSettings.debugMode)
                    Log.Message("Selecting animation for initiators...");
            }
*/
            if (options != null && options.Any())
            {
                if (ShowMeYourHandsModSettings.debugMode)
                    Log.Message("Randomly selecting animation...");
                return options.RandomElement();
            }
            else
                return null;
        }

        public static void RenderPawnHeadMeshInAnimation1(Mesh mesh, Vector3 loc, Quaternion quaternion, Material material, bool drawNow, Pawn pawn)
        {

            if (pawn == null || pawn.Map != Find.CurrentMap)
            {
                GenDraw.DrawMeshNowOrLater(mesh, loc, quaternion, material, drawNow);
                return;
            }

            CompBodyAnimator pawnAnimator = pawn.TryGetComp<CompBodyAnimator>();

            if (pawnAnimator == null || !pawnAnimator.isAnimating)
            {
                GenDraw.DrawMeshNowOrLater(mesh, loc, quaternion, material, drawNow);
            }
            else
            {
                Vector3 pawnHeadPosition = pawnAnimator.getPawnHeadPosition();
                pawnHeadPosition.y = loc.y;
                GenDraw.DrawMeshNowOrLater(MeshPool.humanlikeHeadSet.MeshAt(pawnAnimator.headFacing), pawnHeadPosition, Quaternion.AngleAxis(pawnAnimator.headAngle, Vector3.up), material, true);
            }
        }

        public static void AdjustHead(ref Quaternion quat, ref Rot4 bodyFacing, ref Vector3 pos, ref float angle, Pawn pawn, PawnRenderFlags flags)
        {
            if (flags.FlagSet(PawnRenderFlags.Portrait)) return;

            CompBodyAnimator anim = pawn.TryGetComp<CompBodyAnimator>();
            if (anim.isAnimating)
            {
                bodyFacing = anim.headFacing;
                angle = anim.headAngle;
                quat = Quaternion.AngleAxis(anim.headAngle, Vector3.up);
                pos = anim.getPawnHeadOffset();

            }
        }

        public static void RenderPawnHeadMeshInAnimation(Mesh mesh, Vector3 loc, Quaternion quaternion, Material material, bool portrait, Pawn pawn, float bodySizeFactor = 1)
        {

            if (pawn == null)
            {
                GenDraw.DrawMeshNowOrLater(mesh, loc, quaternion, material, portrait);
                return;
            }

            CompBodyAnimator pawnAnimator = pawn.TryGetComp<CompBodyAnimator>();

            if (pawnAnimator == null || !pawnAnimator.isAnimating || portrait)
            {
                GenDraw.DrawMeshNowOrLater(mesh, loc, quaternion, material, portrait);
            }
            else
            {
                Vector3 pawnHeadPosition = pawnAnimator.getPawnHeadPosition();
                pawnHeadPosition.x *= bodySizeFactor;
                pawnHeadPosition.x *= bodySizeFactor;
                pawnHeadPosition.y = loc.y;
                GenDraw.DrawMeshNowOrLater(mesh, pawnHeadPosition, Quaternion.AngleAxis(pawnAnimator.headAngle, Vector3.up), material, portrait);
            }
        }
#if rjwMerge
        public static bool GenitalCheckForPawn(List<string> requiredGenitals, Pawn pawn, out string failReason)
        {

            failReason = null;
            if (requiredGenitals != null)
            {
                if (requiredGenitals.Contains("Vagina"))
                {

                    if (!rjw.Genital_Helper.has_vagina(pawn))
                    {
                        failReason = "missing vagina";
                        return false;
                    }

                }

                if (requiredGenitals.Contains("Penis"))
                {

                    if (!(rjw.Genital_Helper.has_multipenis(pawn) || rjw.Genital_Helper.has_penis_infertile(pawn) || rjw.Genital_Helper.has_penis_fertile(pawn) || rjw.Genital_Helper.has_ovipositorM(pawn) || rjw.Genital_Helper.has_ovipositorF(pawn)))
                    {
                        failReason = "missing penis";
                        return false;
                    }

                }

                if (requiredGenitals.Contains("Mouth"))
                {

                    if (!rjw.Genital_Helper.has_mouth(pawn))
                    {
                        failReason = "missing mouth";
                        return false;
                    }

                }

                if (requiredGenitals.Contains("Anus"))
                {

                    if (!rjw.Genital_Helper.has_anus(pawn))
                    {
                        failReason = "missing anus";
                        return false;
                    }

                }

                if (requiredGenitals.Contains("Breasts"))
                {
                    if (!rjw.Genital_Helper.can_do_breastjob(pawn))
                    {
                        failReason = "missing breasts";
                        return false;
                    }
                }

                if (requiredGenitals.Contains("NoVagina"))
                {

                    if (rjw.Genital_Helper.has_vagina(pawn))
                    {
                        failReason = "has vagina";
                        return false;
                    }

                }

                if (requiredGenitals.Contains("NoPenis"))
                {

                    if ((rjw.Genital_Helper.has_multipenis(pawn) || rjw.Genital_Helper.has_penis_infertile(pawn) || rjw.Genital_Helper.has_penis_fertile(pawn)))
                    {
                        failReason = "has penis";
                        return false;
                    }

                }

                if (requiredGenitals.Contains("NoMouth"))
                {

                    if (rjw.Genital_Helper.has_mouth(pawn))
                    {
                        failReason = "has mouth";
                        return false;
                    }

                }

                if (requiredGenitals.Contains("NoAnus"))
                {

                    if (rjw.Genital_Helper.has_anus(pawn))
                    {
                        failReason = "has anus";
                        return false;
                    }

                }

                if (requiredGenitals.Contains("NoBreasts"))
                {
                    if (rjw.Genital_Helper.can_do_breastjob(pawn))
                    {
                        failReason = "has breasts";
                        return false;
                    }
                }
            }

            return true;

        }
#endif
        public static Rot4 PawnHeadRotInAnimation(Pawn pawn, Rot4 regularPos)
        {
            Debug.Log("Test");

            if (pawn?.TryGetComp<CompBodyAnimator>() != null && pawn.TryGetComp<CompBodyAnimator>().isAnimating)
            {
                return pawn.TryGetComp<CompBodyAnimator>().headFacing;
            }

            return regularPos;
        }
    }
}
