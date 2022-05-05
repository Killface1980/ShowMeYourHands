using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using ShowMeYourHands;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FacialStuff
{
    [ShowMeYourHandsMod.HotSwappable]
    [StaticConstructorOnStartup]
    public static class PawnExtensions
    {
        public static readonly Dictionary<Pawn, float> pawnBodySizes = new();
        public const string PathHumanlike = "Things/Pawn/Humanlike/";
        public const string PathAnimals = "Things/Pawn/Animal/";
        public const string STR_Foot = "_Foot";
        public const string STR_Hand = "_Hand";
        public static Dictionary<Thing, Color> colorDictionary;

        public static bool ShowWeaponOpenly(this Pawn pawn)
        {
            return pawn.carryTracker?.CarriedThing == null && pawn.equipment?.Primary != null &&
                   (pawn.Drafted ||
                    (pawn.CurJob != null && pawn.CurJob.def.alwaysShowWeapon) ||
                    (pawn.mindState.duty != null && pawn.mindState.duty.def.alwaysShowWeapon));
        }
        static float piHalf = Mathf.PI / 2f;
        static float angleReduce = 0.5f;
        static float angleToPos = 0.01f;
        public enum tweenType { line, sin }
        static public Rot4 Rot90(Rot4 rot)
        {
            if (rot == Rot4.East)
            {
                return Rot4.South;
            }

            if (rot == Rot4.South)
            {
                return Rot4.West;
            }

            if (rot == Rot4.West)
            {
                return Rot4.North;
            }

            return Rot4.East;
        }
        static public Rot4 Rot90b(Rot4 rot)
        {
            if (rot == Rot4.East)
            {
                return Rot4.North;
            }

            if (rot == Rot4.North)
            {
                return Rot4.West;
            }

            if (rot == Rot4.West)
            {
                return Rot4.South;
            }

            return Rot4.East;
        }
        static public bool Ani(ref int tick, int duration, ref float angle, float s_angle, float t_angle, float centerY, ref Vector3 pos, Vector3 s_pos, Vector3 t_pos, Rot4? rot = null, tweenType tween = tweenType.sin, Rot4? axis = null)
        {
            if (tick >= duration)
            {
                tick -= duration;
                return false;
            }

            bool needCenterCheck = true; ;
            if (axis != null)
            {
                if (rot != null)
                {
                    if (rot == Rot4.West)
                    {
                        s_angle = -s_angle;
                        t_angle = -t_angle;
                        s_pos = new Vector3(-s_pos.x, 0f, s_pos.z);
                        t_pos = new Vector3(-t_pos.x, 0f, t_pos.z);
                    }
                }
                if (axis != Rot4.South)
                {
                    needCenterCheck = false;
                }
                if (axis == Rot4.North)
                {
                    //s_angle = -s_angle;
                    //t_angle = -t_angle;
                    s_pos = new Vector3(-s_pos.x, 0f, -s_pos.z);
                    t_pos = new Vector3(-t_pos.x, 0f, -t_pos.z);
                    if (centerY != 0f)
                    {
                        s_pos += new Vector3(s_angle * 0.01f * centerY, 0f, 0f);
                        t_pos += new Vector3(t_angle * 0.01f * centerY, 0f, 0f);
                    }
                }
                else if (axis == Rot4.West)
                {
                    s_pos = new Vector3(s_pos.z, 0f, -s_pos.x);
                    t_pos = new Vector3(t_pos.z, 0f, -t_pos.x);
                    if (centerY != 0f)
                    {
                        s_pos += new Vector3(0f, 0f, s_angle * 0.01f * centerY);
                        t_pos += new Vector3(0f, 0f, t_angle * 0.01f * centerY);
                    }
                }
                else if (axis == Rot4.East)
                {
                    s_pos = new Vector3(-s_pos.z, 0f, s_pos.x);
                    t_pos = new Vector3(-t_pos.z, 0f, t_pos.x);
                    if (centerY != 0f)
                    {
                        s_pos += new Vector3(0f, 0f, -s_angle * 0.01f * centerY);
                        t_pos += new Vector3(0f, 0f, -t_angle * 0.01f * centerY);
                    }
                }


            }
            else if (rot != null)
            {
                if (rot == Rot4.West)
                {
                    s_angle = -s_angle;
                    t_angle = -t_angle;
                    s_pos = new Vector3(-s_pos.x, 0f, s_pos.z);
                    t_pos = new Vector3(-t_pos.x, 0f, t_pos.z);
                }
                else if ((Rot4)rot == Rot4.South)
                {
                    s_angle *= angleReduce;
                    t_angle *= angleReduce;
                    s_pos = new Vector3(0f, 0f, s_pos.z - s_pos.x - s_angle * angleToPos);
                    t_pos = new Vector3(0f, 0f, t_pos.z - t_pos.x - t_angle * angleToPos);
                }
                else if ((Rot4)rot == Rot4.North)
                {
                    s_angle *= -angleReduce;
                    t_angle *= -angleReduce;
                    s_pos = new Vector3(0f, 0f, s_pos.z + s_pos.x - s_angle * angleToPos);
                    t_pos = new Vector3(0f, 0f, t_pos.z + t_pos.x - t_angle * angleToPos);
                }

            }
            if (needCenterCheck && centerY != 0f)
            {
                s_pos += new Vector3(s_angle * -0.01f * centerY, 0f, 0f);
                t_pos += new Vector3(t_angle * -0.01f * centerY, 0f, 0f);
            }



            float tickPer = 0f;
            switch (tween)
            {
                default:
                    tickPer = (tick / (float)duration);
                    break;
                case tweenType.sin:
                    tickPer = Mathf.Sin(piHalf * (tick / (float)duration));
                    break;
            }


            angle += s_angle + (t_angle - s_angle) * tickPer;
            if (s_pos != null)
            {
                pos += s_pos + (t_pos - s_pos) * tickPer;
            }
            return true;
        }

        static public bool Ani(ref int tick, int duration, ref float angle, float s_angle, float t_angle, float centerY, ref Vector3 pos, Rot4? rot = null, tweenType tween = tweenType.sin)
        {
            return Ani(ref tick, duration, ref angle, s_angle, t_angle, centerY, ref pos, Vector3.zero, Vector3.zero, rot, tween);
        }
        static public bool Ani(ref int tick, int duration, ref float angle, ref Vector3 pos, Vector3 s_pos, Vector3 t_pos, Rot4? rot = null, tweenType tween = tweenType.sin)
        {

            return Ani(ref tick, duration, ref angle, 0f, 0f, 0f, ref pos, s_pos, t_pos, rot, tween);
        }

        static public bool AnimationHasTicksLeft(ref int tick, int duration)
        {
            if (tick >= duration)
            {
                tick -= duration;
                return false;
            }
            return true;
        }
/*
        public static float cellCostMin = 999999f;
        public static float cellCostMax = 0f;
        public static float costToPayMin = 99999f;
        public static float costToPayMax = 0f;

        public static float TicksPerMoveCardinalMin = 9999999999f;
        public static float TicksPerMoveCardinalMax = 0f;
        */
        static List<LordJob_Ritual> ar_lordJob_ritual = new();
        static public LordJob_Ritual GetPawnRitual(this Pawn p)
        {
            ar_lordJob_ritual = Find.IdeoManager.GetActiveRituals(p.Map);
            if (ar_lordJob_ritual == null)
            {
                return null;
            }

            foreach (LordJob_Ritual l in ar_lordJob_ritual)
            {
                if (l.PawnsToCountTowardsPresence.Contains(p))
                {
                    return l;
                }
            }
            return null;
        }
        public static bool Aiming(this Pawn pawn)
        {
            return pawn.stances.curStance is Stance_Busy stanceBusy && !stanceBusy.neverAimWeapon &&
                   stanceBusy.focusTarg.IsValid;
        }

        public static bool Fleeing(this Pawn pawn)
        {
            Job job = pawn.CurJob;
            return pawn.MentalStateDef == MentalStateDefOf.PanicFlee
                || (job != null && (job.def == JobDefOf.Flee || job.def == JobDefOf.FleeAndCower));
        }

        [CanBeNull]
        public static CompBodyAnimator GetCompAnim([NotNull] this Pawn pawn)
        {
            return pawn.GetComp<CompBodyAnimator>();
        }

        public static bool GetCompAnim([NotNull] this Pawn pawn, [NotNull] out CompBodyAnimator compAnim)
        {
            compAnim = pawn.GetComp<CompBodyAnimator>();
            return compAnim != null;
        }

        /*
        [CanBeNull]
        public static CompFace GetCompFace([NotNull] this Pawn pawn)
        {
            return pawn.GetComp<CompFace>();
        }
        public static bool GetCompFace([NotNull] this Pawn pawn, [NotNull] out CompFace compFace)
        {
            compFace = pawn.GetComp<CompFace>();
            return compFace != null;
        }
        */
        /*
        public static bool GetPawnFace([NotNull] this Pawn pawn, [CanBeNull] out PawnFace pawnFace)
        {
            pawnFace = null;

            if (!pawn.GetCompFace(out CompFace compFace))
            {
                return false;
            }

            PawnFace face = compFace.PawnFace;
            if (face != null)
            {
                pawnFace = face;
                return true;
            }

            return false;
        }
        */

        public static bool HasCompAnimator([NotNull] this Pawn pawn)
        {
            return pawn.def.HasComp(typeof(CompBodyAnimator));
        }


        /*
        public static bool HasPawnFace([NotNull] this Pawn pawn)
        {
            if (pawn.GetCompFace(out CompFace compFace))
            {
                PawnFace face = compFace.PawnFace;
                return face != null;
            }

            return false;
        }
        */
    }
}