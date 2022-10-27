using PawnAnimator.Defs;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PawnAnimator
{
    [PawnAnimatorMod.HotSwappable]
    public class GameComponent_FacialStuff : GameComponent
    {
        #region Public Constructors

        public GameComponent_FacialStuff()
        {
        }

        // ReSharper disable once UnusedParameter.Local
        public GameComponent_FacialStuff(Game game)
        {
            if (!RimWorld_MainMenuDrawer_MainMenuOnGUI.alreadyRun)
            {
                RimWorld_MainMenuDrawer_MainMenuOnGUI.MainMenuOnGUI();
            }

            // todo: use BodyDef instead, target for kickstarting?

            SetMainButtons();
            // BuildWalkCycles();

            // foreach (BodyAnimDef def in DefDatabase<BodyAnimDef>.AllDefsListForReading)
            // {
            // if (def.walkCycles.Count == 0)
            // {
            // var length = Enum.GetNames(typeof(LocomotionUrgency)).Length;
            // for (int index = 0; index < length; index++)
            // {
            // WalkCycleDef cycleDef = new WalkCycleDef();
            // cycleDef.defName = def.defName + "_cycle";
            // def.walkCycles.Add(index, cycleDef);
            // }
            // }
            // }
        }

        #endregion Public Constructors

        #region Public Methods

        public static void SetMainButtons()
        {
            MainButtonDef button = DefDatabase<MainButtonDef>.GetNamedSilentFail("WalkAnimator");
            //   MainButtonDef button2 = DefDatabase<MainButtonDef>.GetNamedSilentFail("PoseAnimator");
            if (button != null)
            {
                button.buttonVisible = Prefs.DevMode;
            }
        }

        public static void BuildWalkCycles([CanBeNull] WalkCycleDef defToRebuild = null)
        {
            List<WalkCycleDef> cycles = new();
            if (defToRebuild != null)
            {
                cycles.Add(defToRebuild);
            }
            else
            {
                cycles = DefDatabase<WalkCycleDef>.AllDefsListForReading;
            }

            if (cycles == null)
            {
                return;
            }

            for (int index = 0; index < cycles.Count; index++)
            {
                WalkCycleDef cycle = cycles[index];
                if (cycle != null)
                {
                    cycle.HeadAngleX = new SimpleCurve();
                    cycle.HeadOffsetZ = new SimpleCurve();

                    cycle.BodyAngle = new SimpleCurve();
                    cycle.BodyAngleVertical = new SimpleCurve();
                    cycle.BodyOffsetZ = new SimpleCurve();

                    // cycle.BodyOffsetVerticalZ = new SimpleCurve();
                    cycle.FootAngle = new SimpleCurve();
                    cycle.FootPositionX = new SimpleCurve();
                    cycle.FootPositionZ = new SimpleCurve();

                    // cycle.FootPositionVerticalZ = new SimpleCurve();
                    cycle.HandsSwingAngle = new SimpleCurve();
                    cycle.HandsSwingPosVertical = new SimpleCurve();
                    cycle.ShoulderOffsetHorizontalX = new SimpleCurve();
                    cycle.HipOffsetHorizontalX = new SimpleCurve();

                    // Quadrupeds
                    cycle.FrontPawAngle = new SimpleCurve();
                    cycle.FrontPawPositionX = new SimpleCurve();
                    cycle.FrontPawPositionZ = new SimpleCurve();

                    // cycle.FrontPawPositionVerticalZ = new SimpleCurve();
                    if (cycle.keyframes.NullOrEmpty())
                    {
                        cycle.keyframes = new List<PawnKeyframe>();
                        for (int i = 0; i < 9; i++)
                        {
                            cycle.keyframes.Add(new PawnKeyframe(i));
                        }
                    }

                    // Log.Message(cycle.defName + " has " + cycle.animation.Count);
                    foreach (PawnKeyframe key in cycle.keyframes)
                    {
                        BuildAnimationKeys(key, cycle);
                    }
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static void BuildAnimationKeys(PawnKeyframe key, WalkCycleDef cycle)
        {
            List<PawnKeyframe> keyframes = cycle.keyframes;

            List<PawnKeyframe> autoKeys = keyframes.Where(x => x.Status != KeyStatus.Manual).ToList();

            List<PawnKeyframe> manualKeys = keyframes.Where(x => x.Status == KeyStatus.Manual).ToList();

            float autoFrames = (float)key.KeyIndex / (autoKeys.Count - 1);

            float frameAt;

            // Distribute manual keys
            if (!manualKeys.NullOrEmpty())
            {
                frameAt = (float)key.KeyIndex / (autoKeys.Count - 1);
                float divider = (float)1 / (autoKeys.Count - 1);
                float? shift = manualKeys.Find(x => x.KeyIndex == key.KeyIndex)?.Shift;
                if (shift.HasValue)
                {
                    frameAt += divider * shift.Value;
                }
            }
            else
            {
                frameAt = (float)key.KeyIndex / (keyframes.Count - 1);
            }

            Dictionary<SimpleCurve, float?> dict = new()
            {
                { cycle.HeadAngleX, key.HeadAngleX },
                { cycle.HeadOffsetZ, key.HeadOffsetZ },
                {
                    cycle.ShoulderOffsetHorizontalX,
                    key.ShoulderOffsetHorizontalX
                },
                {
                    cycle.HipOffsetHorizontalX,
                    key.HipOffsetHorizontalX
                },
                {
                    cycle.BodyAngle,
                    key.BodyAngle
                },
                {
                    cycle.BodyAngleVertical,
                    key.BodyAngleVertical
                },
                {
                    cycle.BodyOffsetZ,
                    key.BodyOffsetZ
                },
                {
                    cycle.FootAngle,
                    key.FootAngle
                },
                {
                    cycle.FootPositionX,
                    key.FootPositionX
                },
                {
                    cycle.FootPositionZ,
                    key.FootPositionZ
                },
                {
                    cycle.HandsSwingAngle,
                    key.HandsSwingAngle
                },
                {
                    cycle.HandsSwingPosVertical,
                    key.HandsSwingAngle
                },
                {
                    cycle.FrontPawAngle,
                    key.FrontPawAngle
                },
                {
                    cycle.FrontPawPositionX,
                    key.FrontPawPositionX
                },
                {
                    cycle.FrontPawPositionZ,
                    key.FrontPawPositionZ
                }

                // { cycle.BodyOffsetVerticalZ, key.BodyOffsetVerticalZ },

                // { cycle.FootPositionVerticalZ, key.FootPositionVerticalZ },

                // { cycle.HandsSwingPosVertical, key.HandsSwingPosVertical },
                // {
                // cycle.FrontPawPositionVerticalZ,
                // key.FrontPawPositionVerticalZ
                // }
            };

            foreach (KeyValuePair<SimpleCurve, float?> pair in dict)
            {
                UpdateCurve(key, pair.Value, pair.Key, frameAt);
            }
        }

        private static void UpdateCurve(PawnKeyframe key, float? curvePoint, SimpleCurve simpleCurve, float frameAt)
        {
            if (curvePoint.HasValue)
            {
                simpleCurve.Add(frameAt, curvePoint.Value);
            }
            else
            {
                // No value at 0 => add points to prevent the curve from bugging out
                if (key.KeyIndex == 0)
                {
                    simpleCurve.Add(0, 0);
                    simpleCurve.Add(1, 0);
                }
            }
        }

        #endregion Private Methods
    }
}