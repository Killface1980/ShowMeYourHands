using PawnAnimator.Defs;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using PawnAnimator.Animator;
using PawnAnimator.FSWalking.Animator;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnAnimator.AnimatorWindows
{
    [PawnAnimatorMod.HotSwappable]
    public class MainTabWindow_WalkAnimator : MainTabWindow_BaseAnimator
    {
        #region Public Fields

        public bool Equipment;
        public float HorHeadOffset;
        public float VerHeadOffset;

        #endregion Public Fields

        #region Public Properties


        protected override void SetKeyframes()
        {
            PawnKeyframes = this.EditorWalkcycle.keyframes;
            this.Label = this.EditorWalkcycle.LabelCap;
        }
        #endregion Public Properties

        #region Private Properties

        // public static float verHeadOffset;

        #endregion Private Properties

        #region Public Methods

        private string FilePathBodyanim
        {
            get
            {
                return DefPath + "/BodyAnimDefs/" + this.CurrentBodyAnimDef.defName + ".xml";
            }
        }

        private string PathWalkcycles
        {
            get
            {
                return DefPath + "/WalkCycleDefs/" + this.EditorWalkcycle.defName + ".xml";

            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            if (GUI.changed)
            {
                if (!this.Loop)
                {
                    GameComponent_FacialStuff.BuildWalkCycles();
                }
            }
        }

        public override void PostClose()
        {
            base.PostClose();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            // IsMoving = true;
        }

        protected override void BuildEditorCycle()
        {
            base.BuildEditorCycle();
            GameComponent_FacialStuff.BuildWalkCycles(this.EditorWalkcycle);
        }

        // public static float horHeadOffset;
        protected override void DoBasicSettingsMenu([NotNull] Listing_Standard listing)
        {
            base.DoBasicSettingsMenu(listing);

            //  listing.CheckboxLabeled("Moving", ref IsMoving);

            // listing_Standard.CheckboxLabeled("Equipment", ref Equipment);

            // listing_Standard.Label(horHeadOffset.ToString("N2") + " - " + verHeadOffset.ToString("N2"));
            // horHeadOffset = listing_Standard.Slider(horHeadOffset, -1f, 1f);
            // verHeadOffset = listing_Standard.Slider(verHeadOffset, -1f, 1f);
            listing.Label(this.CurrentBodyAnimDef.offCenterX.ToString("N2"));
            this.CurrentBodyAnimDef.offCenterX = listing.Slider(this.CurrentBodyAnimDef.offCenterX, -0.2f, 0.2f);

            if (listing.ButtonText("This pawn is using: " + this.CurrentBodyAnimDef.WalkCycleType))
            {
                List<FloatMenuOption> list = new();

                List<WalkCycleDef> listy = DefDatabase<WalkCycleDef>.AllDefsListForReading;

                List<string> stringsy = new();

                foreach (WalkCycleDef cycleDef in listy)
                {
                    if (!stringsy.Contains(cycleDef.WalkCycleType))
                    {
                        stringsy.Add(cycleDef.WalkCycleType);
                    }
                }

                foreach (string s in stringsy)
                {
                    list.Add(new FloatMenuOption(s, delegate { this.CurrentBodyAnimDef.WalkCycleType = s; }));
                }

                Find.WindowStack.Add(new FloatMenu(list));
            }

            if (listing.ButtonText(this.EditorWalkcycle.LabelCap))
            {
                List<string> exists = new();
                List<FloatMenuOption> list = new();
                this.CurrentBodyAnimDef.walkCycles.Clear();

                foreach (WalkCycleDef walkcycle in (DefDatabase<WalkCycleDef>.AllDefs.OrderBy(bsm => bsm.label))
                                                  .TakeWhile(current => this.CurrentBodyAnimDef.WalkCycleType != "None")
                                                  .Where(current => current.WalkCycleType ==
                                                                    this.CurrentBodyAnimDef.WalkCycleType))
                {
                    list.Add(new FloatMenuOption(walkcycle.LabelCap, delegate { this.EditorWalkcycle = walkcycle; }));
                    exists.Add(walkcycle.locomotionUrgency.ToString());
                    this.CurrentBodyAnimDef.walkCycles.Add(walkcycle.locomotionUrgency, walkcycle);
                }

                string[] names = Enum.GetNames(typeof(LocomotionUrgency));
                for (int index = 0; index < names.Length; index++)
                {
                    string name = names[index];
                    LocomotionUrgency myenum = (LocomotionUrgency)Enum.ToObject(typeof(LocomotionUrgency), index);

                    if (exists.Contains(myenum.ToString()))
                    {
                        continue;
                    }

                    list.Add(
                             new FloatMenuOption(
                                                 "Add new " + this.CurrentBodyAnimDef.WalkCycleType + "_" + myenum,
                                                 delegate
                                                 {
                                                     WalkCycleDef newCycle = new();
                                                     newCycle.defName =
                                                     newCycle.label =
                                                     this.CurrentBodyAnimDef.WalkCycleType + "_" + name;
                                                     newCycle.locomotionUrgency = myenum;
                                                     newCycle.WalkCycleType = this.CurrentBodyAnimDef.WalkCycleType;
                                                     GameComponent_FacialStuff.BuildWalkCycles(newCycle);
                                                     this.EditorWalkcycle = newCycle;
                                                     this.CurrentBodyAnimDef.walkCycles.Add(myenum, newCycle);
                                                 }));
                }

                Find.WindowStack.Add(new FloatMenu(list));
            }

            listing.Gap();
            if (listing.ButtonText("Export BodyDef"))
            {
                Find.WindowStack.Add(
                    Dialog_MessageBox.CreateConfirmation(
                        "Confirm overwriting " +
                        this.FilePathBodyanim,
                        delegate
                        {
                            ExportAnimDefs.Defs animDef =
                                new(this.CurrentBodyAnimDef);

                            DirectXmlSaver.SaveDataObject(
                                animDef,
                                this.FilePathBodyanim);
                        },
                        true));

                // BodyAnimDef animDef = this.bodyAnimDef;
            }

            if (listing.ButtonText("Export WalkCycle"))
            {

                Find.WindowStack.Add(
                    Dialog_MessageBox.CreateConfirmation(
                        "Confirm exporting " + this.PathWalkcycles + ".\nExisting WalkCycleDef will be overwritten.",
                        delegate
                        {
                            ExportWalkCycleDefs.Defs cycle =
                                new(this.EditorWalkcycle);

                            DirectXmlSaver.SaveDataObject(
                                cycle,
                                this.PathWalkcycles);
                        },
                        true));
            }
        }
         protected override void AddPortraitWidget(float inRectWidth)
        {
            base.AddPortraitWidget(inRectWidth);

            Rect rect = new(0, 0, inRectWidth, inRectWidth);

            // Portrait

            if (false)
            {

                FS_Skeleton skeleton = new() { joints = new List<FS_Joint>() };
                FS_Joint head = new() { color = Color.red };
                FS_Joint neck = new() { color = Color.cyan };
                FS_Joint hipCenter = new() { color = Color.cyan };

                FS_Joint leftShoulder = new();
                FS_Joint leftElbow = new();
                FS_Joint leftWrist = new();
                FS_Joint leftHand = new();

                FS_Joint rightShoulder = new();
                FS_Joint rightElbow = new();
                FS_Joint rightWrist = new();
                FS_Joint rightHand = new();

                FS_Joint leftHip = new();
                FS_Joint leftKnee = new();
                FS_Joint leftAnkle = new();
                FS_Joint leftFoot = new();

                FS_Joint rightHip = new();
                FS_Joint rightKnee = new();
                FS_Joint rightAnkle = new();
                FS_Joint rightFoot = new();


                skeleton.joints.Add(head);
            }


            // Draw the pawn's portrait
            Vector2 size = new(rect.width / 1.4f, rect.height); // 128x180

            Rect position = new(
                rect.width * 0.5f - size.x * 0.5f,
                rect.height * 0.5f - size.y * 0.5f - 10f,
                size.x,
                size.y);


            Vector3 cameraOffset = new(0f, 0f, 0.1f + this.EditorWalkcycle.BodyOffsetZ.Evaluate(AnimationPercent));
            cameraOffset.z -= this.CurrentBodyAnimDef.extraLegLength;

            float currentAngle = 0f;

            if (CurrentRotation.IsHorizontal)
            {
                if (this.EditorWalkcycle.BodyAngle.PointsCount > 0)
                {
                    currentAngle = (CurrentRotation == Rot4.West ? -1 : 1)
                                   * this.EditorWalkcycle.BodyAngle.Evaluate(AnimationPercent);
                }
            }
            else
            {
                if (this.EditorWalkcycle.BodyAngleVertical.PointsCount > 0)
                {
                    currentAngle = (CurrentRotation == Rot4.South ? -1 : 1)
                                   * this.EditorWalkcycle.BodyAngleVertical.Evaluate(AnimationPercent);
                }
            }

            // RenderTexture image = PortraitsCache.Get(Pawn, size, cameraOffset, this.Zoom);
            RenderTexture renderTexture = new((int)size.x, (int)size.y, 24);
            Find.PawnCacheRenderer.RenderPawn(pawn, renderTexture, cameraOffset, this.Zoom, currentAngle, CurrentRotation);
            GUI.DrawTexture(position, renderTexture);
            renderTexture.Release();

            float percentInverse = AnimationPercent;
            if (percentInverse <= 0.5f)
            {
                percentInverse += 0.5f;
            }
            else
            {
                percentInverse -= 0.5f;
            }

            // rect in center
            Rect leftFootRect = new(position.x + position.width/4, position.y + position.height/2, position.width/2, position.width/2);

            //position it
            Rect rightFootRect = new(leftFootRect);

            leftFootRect.x += this.CurrentBodyAnimDef.hipOffsets[CurrentRotation.AsInt].x * inRectWidth/1.5f;
            rightFootRect.x -= this.CurrentBodyAnimDef.hipOffsets[CurrentRotation.AsInt].x * inRectWidth/1.5f;

            leftFootRect.x += this.EditorWalkcycle.FootPositionX.Evaluate(percentInverse) * inRectWidth / 1.5f;
            rightFootRect.x += this.EditorWalkcycle.FootPositionX.Evaluate(AnimationPercent ) * inRectWidth / 1.5f;
            leftFootRect.y -= this.EditorWalkcycle.FootPositionZ.Evaluate(percentInverse) * inRectWidth / 1.5f;
            rightFootRect.y -= this.EditorWalkcycle.FootPositionZ.Evaluate(AnimationPercent ) * inRectWidth / 1.5f;


            Vector3 rightFootVector = Vector3.zero;
            Vector3 leftFootVector  = Vector3.zero;
            float footAngleRight    = 0;
            float footAngleLeft     = 0;
            float offsetJoint       = 0;

            footAngleLeft  += this.EditorWalkcycle.FootAngle.Evaluate(percentInverse);
            footAngleRight += this.EditorWalkcycle.FootAngle.Evaluate(AnimationPercent);



            Matrix4x4 matrix = GUI.matrix;


            GUI.matrix = matrix;

            // Draw Feet
            Material rightFootMat   = this.CompAnim.pawnBodyGraphic?.FootGraphicRight?.MatAt(CurrentRotation);
            Material leftFootMat    = this.CompAnim.pawnBodyGraphic?.FootGraphicLeft?.MatAt(CurrentRotation);
            Material leftShadowMat  = this.CompAnim.pawnBodyGraphic?.FootGraphicLeftShadow?.MatAt(CurrentRotation);
            Material rightShadowMat = this.CompAnim.pawnBodyGraphic?.FootGraphicRightShadow?.MatAt(CurrentRotation);

          //  if (EditorWalkcycle.FootAngle.PointsCount > 0)
            {
                 GUIUtility.RotateAroundPivot(footAngleLeft, leftFootRect.center);
                 GUI.DrawTexture(leftFootRect, leftFootMat.mainTexture);
                 GUI.matrix = matrix;
                 GUIUtility.RotateAroundPivot(footAngleRight, rightFootRect.center);
                 GUI.DrawTexture(rightFootRect, rightFootMat.mainTexture);
                 GUI.matrix = matrix;
            }




            //GUI.DrawTexture(position, PortraitsCache.Get(pawn, size, BodyRot, new Vector3(0f, 0f, 0.1f), this.Zoom));

            // GUI.DrawTexture(position, PortraitsCache.Get(pawn, size, default(Vector3)));
            // Widgets.DrawBox(rect);
        }

         private static void AddVec3ToRect(ref Rect rect, Vector3 vec3)
         {
             rect.x += vec3.x*3;
             rect.y += vec3.z*3;
         }

         protected override void DrawBackground(Rect rect)
        {
            GUI.BeginGroup(rect);
            _ = AnimationPercent;
            float width = rect.width;
            _ = rect.x;

            float moved = (width * AnimationPercent);
            Rect rect1;
            Rect rect2;
            if (CurrentRotation == Rot4.East)
            {
                rect1 = new Rect(rect) { x = rect.x - moved };
                rect2 = new Rect(rect1) { x = rect1.xMax };
            }
            else if (CurrentRotation == Rot4.West)
            {
                rect1 = new Rect(rect) { x = rect.x + moved };
                rect2 = new Rect(rect1) { x = rect1.xMin - width };
            }
            else if (CurrentRotation == Rot4.North)
            {
                rect1 = new Rect(rect) { y = rect.y + moved };
                rect2 = new Rect(rect1) { y = rect1.yMin - width };
            }
            else
            {
                rect1 = new Rect(rect) { y = rect.y - moved };
                rect2 = new Rect(rect1) { y = rect1.yMax };
            }

            GUI.DrawTexture(rect1, AnimatorTextures.BackgroundAnimTex);
            GUI.DrawTexture(rect2, AnimatorTextures.BackgroundAnimTex);
            GUI.EndGroup();
        }

        protected override void DrawBodySettingsEditor(Rot4 rotation)
        {
            Rect sliderRect = new(0, 0, this.SliderWidth, 40f);

            // this.DrawBodyStats("legLength", ref bodyAnimDef.legLength, ref sliderRect);
            // this.DrawBodyStats("hipOffsetVerticalFromCenter",
            // ref bodyAnimDef.hipOffsetVerticalFromCenter, ref sliderRect);

            Vector2 headOffset = this.CurrentBodyAnimDef.headOffset;
            DrawBodyStats("headOffsetX", ref headOffset.x, ref sliderRect);
            DrawBodyStats("headOffsetY", ref headOffset.y, ref sliderRect);


            Vector3 shoulderOffset = this.CurrentBodyAnimDef.shoulderOffsets[rotation.AsInt];

            if (shoulderOffset.y == 0f)
            {
                if (rotation == Rot4.West)
                {
                    shoulderOffset.y = -0.025f;
                }
                else
                {
                    shoulderOffset.y = 0.025f;
                }
            }

            bool front = shoulderOffset.y > 0;

            if (rotation == Rot4.West)
            {
                front = shoulderOffset.y < 0;
            }

            DrawBodyStats("shoulderOffsetX", ref shoulderOffset.x, ref sliderRect);
            DrawBodyStats("shoulderOffsetZ", ref shoulderOffset.z, ref sliderRect);
            // this.DrawBodyStats("shoulderFront",   ref front,            ref sliderRect);

            Vector3 hipOffset = this.CurrentBodyAnimDef.hipOffsets[rotation.AsInt];
            if (hipOffset.y == 0f)
            {
                if (rotation == Rot4.West)
                {
                    hipOffset.y = -0.025f;
                }
                else
                {
                    hipOffset.y = 0.025f;
                }
            }

            bool hipFront = hipOffset.y > 0;
            if (rotation == Rot4.West)
            {
                hipFront = hipOffset.y < 0;
            }

            DrawBodyStats("hipOffsetX", ref hipOffset.x, ref sliderRect);
            DrawBodyStats("hipOffsetZ", ref hipOffset.z, ref sliderRect);
            // this.DrawBodyStats("hipFront",   ref hipFront,    ref sliderRect);

            if (GUI.changed)
            {
                this.CurrentBodyAnimDef.headOffset = headOffset;
                SetNewVector(rotation, shoulderOffset, this.CurrentBodyAnimDef.shoulderOffsets, front);
                SetNewVector(rotation, hipOffset, this.CurrentBodyAnimDef.hipOffsets, hipFront);
            }

            DrawBodyStats("armLength", ref this.CurrentBodyAnimDef.armLength, ref sliderRect);
            DrawBodyStats("extraLegLength", ref this.CurrentBodyAnimDef.extraLegLength, ref sliderRect);
        }

        protected override void DrawKeyframeEditor(Rect keyframes, Rot4 rotation)
        {
            if (CurrentFrame == null)
            {
                return;
            }

            Rect leftController = keyframes.LeftHalf();
            Rect rightController = keyframes.RightHalf();
            leftController.xMax -= this.Spacing;

            rightController.xMin += this.Spacing;
            {
                GUI.BeginGroup(leftController);
                Rect editorRect = new(0f, 0f, leftController.width, 56f);

                // Dictionary<int, float> keysFloats = new Dictionary<int, float>();

                // // Get the next keyframe
                // for (int i = 0; i < frames.Count; i++)
                // {
                // float? footPositionX = frames[i].FootPositionX;
                // if (!footPositionX.HasValue)
                // {
                // continue;
                // }
                // keysFloats.Add(frames[i].KeyIndex, footPositionX.Value);
                // }
                List<int> framesAt;
                List<PawnKeyframe> frames = PawnKeyframes;
                WalkCycleDef walkcycle = this.EditorWalkcycle;
                {
                    framesAt = (from keyframe in frames where keyframe.FootPositionX.HasValue select keyframe.KeyIndex)
                   .ToList();


                    this.SetPosition(
                                     ref CurrentFrame.FootPositionX,
                                     ref editorRect,
                                     walkcycle.FootPositionX,
                                     "FootPosX",
                                     framesAt);

                    framesAt = (from keyframe in frames where keyframe.FootPositionZ.HasValue select keyframe.KeyIndex)
                   .ToList();

                    this.SetPosition(
                                     ref CurrentFrame.FootPositionZ,
                                     ref editorRect,
                                     walkcycle.FootPositionZ,
                                     "FootPosY",
                                     framesAt);

                    framesAt = (from keyframe in frames where keyframe.FootAngle.HasValue select keyframe.KeyIndex)
                   .ToList();

                    this.SetAngle(
                                  ref CurrentFrame.FootAngle,
                                  ref editorRect,
                                  walkcycle.FootAngle,
                                  "FootAngle",
                                  framesAt);

                    framesAt = (from keyframe in frames
                                where keyframe.HipOffsetHorizontalX.HasValue
                                select keyframe.KeyIndex).ToList();

                    this.SetPosition(
                                     ref CurrentFrame.HipOffsetHorizontalX,
                                     ref editorRect,
                                     walkcycle.HipOffsetHorizontalX,
                                     "HipOffsetHorizontalX",
                                     framesAt);

                    // Quadruped
                }

                // else
                // {
                // framesAt = (from keyframe in frames
                // where keyframe.FootPositionVerticalZ.HasValue
                // select keyframe.KeyIndex).ToList();
                // this.SetPosition(
                // ref thisFrame.FootPositionVerticalZ,
                // ref editorRect,
                // EditorWalkcycle.FootPositionVerticalZ,
                // "FootPosVerticalY", framesAt);
                // }
                GUI.EndGroup();

                GUI.BeginGroup(rightController);

                editorRect.x = 0f;
                editorRect.y = 0f;

                if (this.CompAnim.BodyAnim.bipedWithHands)
                {
                    this.SetAngleShoulder(ref walkcycle.shoulderAngle, ref editorRect, "ShoulderAngle");

                    framesAt =
                    (from keyframe in frames where keyframe.HandsSwingAngle.HasValue select keyframe.KeyIndex)
                   .ToList();

                    this.SetAngle(
                                  ref CurrentFrame.HandsSwingAngle,
                                  ref editorRect,
                                  walkcycle.HandsSwingAngle,
                                  "HandSwing",
                                  framesAt);
                }

                if (rotation.IsHorizontal)
                {
                    if (this.CompAnim.BodyAnim.quadruped)
                    {
                        framesAt = (from keyframe in frames
                                    where keyframe.FrontPawPositionX.HasValue
                                    select keyframe.KeyIndex).ToList();
                        this.SetPosition(
                                         ref CurrentFrame.FrontPawPositionX,
                                         ref editorRect,
                                         walkcycle.FrontPawPositionX,
                                         "FrontPawPositionX",
                                         framesAt);

                        framesAt = (from keyframe in frames
                                    where keyframe.FrontPawPositionZ.HasValue
                                    select keyframe.KeyIndex).ToList();

                        this.SetPosition(
                                         ref CurrentFrame.FrontPawPositionZ,
                                         ref editorRect,
                                         walkcycle.FrontPawPositionZ,
                                         "FrontPawPositionZ",
                                         framesAt);

                        framesAt = (from keyframe in frames
                                    where keyframe.FrontPawAngle.HasValue
                                    select keyframe.KeyIndex).ToList();

                        this.SetAngle(
                                      ref CurrentFrame.FrontPawAngle,
                                      ref editorRect,
                                      walkcycle.FrontPawAngle,
                                      "FrontPawAngle",
                                      framesAt);
                    }


                    framesAt = (from keyframe in frames where keyframe.BodyAngle.HasValue select keyframe.KeyIndex)
                        .ToList();

                    this.SetAngle(
                                  ref CurrentFrame.BodyAngle,
                                  ref editorRect,
                                  walkcycle.BodyAngle,
                                  "BodyAngle",
                                  framesAt);
                }
                else
                {
                    if (this.CompAnim.BodyAnim.bipedWithHands)
                    {
                        // framesAt = (from keyframe in frames
                        // where keyframe.HandsSwingPosVertical.HasValue
                        // select keyframe.KeyIndex).ToList();
                        // this.SetPosition(
                        // ref thisFrame.HandsSwingPosVertical,
                        // ref editorRect,
                        // EditorWalkcycle.HandsSwingPosVertical,
                        // "HandsSwingPosVertical", framesAt);
                    }

                    // framesAt = (from keyframe in frames
                    // where keyframe.FrontPawPositionVerticalZ.HasValue
                    // select keyframe.KeyIndex).ToList();
                    // if (this.CompAnim.Props.quadruped)
                    // {
                    // this.SetPosition(
                    // ref thisFrame.FrontPawPositionVerticalZ,
                    // ref editorRect,
                    // EditorWalkcycle.FrontPawPositionVerticalZ,
                    // "FrontPawPosVerticalY", framesAt);
                    // }
                    // framesAt = (from keyframe in frames
                    // where keyframe.BodyOffsetVerticalZ.HasValue
                    // select keyframe.KeyIndex).ToList();
                    // this.SetPosition(
                    // ref thisFrame.BodyOffsetVerticalZ,
                    // ref editorRect,
                    // EditorWalkcycle.BodyOffsetVerticalZ,
                    // "BodyOffsetVerticalZ", framesAt);
                    framesAt = (from keyframe in frames
                                where keyframe.BodyAngleVertical.HasValue
                                select keyframe.KeyIndex).ToList();
                    this.SetAngle(
                                  ref CurrentFrame.BodyAngleVertical,
                                  ref editorRect,
                                  walkcycle.BodyAngleVertical,
                                  "BodyAngleVertical",
                                  framesAt);
                }
                framesAt = (from keyframe in frames
                            where keyframe.ShoulderOffsetHorizontalX.HasValue
                            select keyframe.KeyIndex).ToList();
                this.SetPosition(
                                 ref CurrentFrame.ShoulderOffsetHorizontalX,
                                 ref editorRect,
                                 walkcycle.ShoulderOffsetHorizontalX,
                                 "ShoulderOffsetHorizontalX",
                                 framesAt);

                framesAt = (from keyframe in frames where keyframe.HeadAngleX.HasValue select keyframe.KeyIndex)
                    .ToList();

                this.SetPosition(
                    ref CurrentFrame.HeadAngleX,
                    ref editorRect,
                    walkcycle.HeadAngleX,
                    "HeadAngleX",
                    framesAt, 3f);

                framesAt = (from keyframe in frames where keyframe.HeadOffsetZ.HasValue select keyframe.KeyIndex)
                    .ToList();

                this.SetPosition(
                    ref CurrentFrame.HeadOffsetZ,
                    ref editorRect,
                    walkcycle.HeadOffsetZ,
                    "HeadOffsetZ",
                    framesAt);

                framesAt =
                (from keyframe in frames where keyframe.BodyOffsetZ.HasValue select keyframe.KeyIndex).ToList();

                this.SetPosition(
                                 ref CurrentFrame.BodyOffsetZ,
                                 ref editorRect,
                                 walkcycle.BodyOffsetZ,
                                 "BodyOffsetZ",
                                 framesAt);

                GUI.EndGroup();
            }
        }

        protected override void FindRandomPawn()
        {
            //if (Pawn == null)
            {
                base.FindRandomPawn();

                BodyAnimDef anim = this.CompAnim.BodyAnim;
                if (anim != null && anim.walkCycles.Any())
                {
                    this.EditorWalkcycle = anim.walkCycles.FirstOrDefault().Value;
                }
            }
        }

        protected override void SetCurrentCycle()
        {
            BodyAnimDef anim = this.CompAnim.BodyAnim;
            if (anim != null && anim.walkCycles.Any())
            {
                this.EditorWalkcycle = anim.walkCycles.FirstOrDefault().Value;
            }
        }
        #endregion Public Methods

        #region Private Methods

        private static void DrawBodyStats(string label, ref float value, ref Rect sliderRect)
        {
            float left = -1.5f;
            float right = 1.5f;
            value = Widgets.HorizontalSlider(
                                                   sliderRect,
                                                   value,
                                                   left,
                                                   right,
                                                   false,
                                                   label + ": " + value,
                                                   left.ToString(),
                                                   right.ToString(),
                                                   0.025f);

            sliderRect.y += sliderRect.height + 8f;
        }

        #endregion Private Methods
    }
}