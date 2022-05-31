using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FacialStuff;
using Verse;
using RimWorld;
using ShowMeYourHands;
using UnityEngine;

namespace Rimworld_Animations
{
    class MainTabWindow_OffsetConfigure : MainTabWindow
    {

        public override Vector2 RequestedTabSize => new Vector2(505, 380);
        public override void DoWindowContents(Rect inRect)
        {

            Rect position = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);


            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(position);

            listingStandard.Label("Animation Manager");

            listingStandard.GapLine();


            if (Find.Selector.SingleSelectedThing is Pawn)
            {

                Pawn curPawn = Find.Selector.SingleSelectedThing as Pawn;

                if (curPawn.TryGetComp<CompBodyAnimator>().isAnimating)
                {

                    AnimationDef def = curPawn.TryGetComp<CompBodyAnimator>().CurrentAnimation;
                    int ActorIndex = curPawn.TryGetComp<CompBodyAnimator>().ActorIndex;
                    float offsetX = 0, offsetZ = 0, rotation = 0;

                    string bodyTypeDef = (curPawn.story?.bodyType != null) ? curPawn.story.bodyType.ToString() : "";

                    if (ShowMeYourHandsModSettings.offsets.ContainsKey(def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex))
                    {
                        offsetX = ShowMeYourHandsModSettings.offsets[def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex].x;
                        offsetZ = ShowMeYourHandsModSettings.offsets[def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex].y;
                    }
                    else
                    {
                        ShowMeYourHandsModSettings.offsets.Add(def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex, new Vector2(0, 0));
                    }

                    if (ShowMeYourHandsModSettings.rotation.ContainsKey(def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex))
                    {
                        rotation = ShowMeYourHandsModSettings.rotation[def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex];
                    }
                    else
                    {
                        ShowMeYourHandsModSettings.rotation.Add(def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex, 0);
                    }

                    listingStandard.Label("Name: " + curPawn.Name + " Race: " + curPawn.def.defName + " Actor Index: " + curPawn.TryGetComp<CompBodyAnimator>().ActorIndex + " Body Type (if any): " + bodyTypeDef + " Animation: " + def.label + (curPawn.TryGetComp<CompBodyAnimator>().Mirror ? " mirrored" : ""));

                    if (curPawn.def.defName == "Human")
                    {
                        listingStandard.Label("Warning--You generally don't want to change human offsets, only alien offsets");
                    }

                    bool mirrored = curPawn.TryGetComp<CompBodyAnimator>().Mirror;

                    float.TryParse(listingStandard.TextEntryLabeled("X Offset: ", offsetX.ToString()), out offsetX);
                    offsetX = listingStandard.Slider(offsetX, -2 * (mirrored ? -1 : 1), 2 * (mirrored ? -1 : 1));

                    float.TryParse(listingStandard.TextEntryLabeled("Z Offset: ", offsetZ.ToString()), out offsetZ);
                    offsetZ = listingStandard.Slider(offsetZ, -2, 2);

                    float.TryParse(listingStandard.TextEntryLabeled("Rotation: ", rotation.ToString()), out rotation);
                    rotation = listingStandard.Slider(rotation, -180, 180);

                    if (listingStandard.ButtonText("Reset All"))
                    {
                        offsetX = 0;
                        offsetZ = 0;
                        rotation = 0;
                    }

                    listingStandard.GapLine();

                    if (listingStandard.ButtonText("Shift Actors"))
                    {

/*                        if (AnimationSettings.debugMode)
                        {
                            Log.Message("Shifting actors in animation...");
                        }
*/
/*                        for (int i = 0; i < curPawn.TryGetComp<CompBodyAnimator>().actorsInCurrentAnimation.Count; i++)
                        {

                            Pawn actor = curPawn.TryGetComp<CompBodyAnimator>().actorsInCurrentAnimation[i];

                            actor.TryGetComp<CompBodyAnimator>()?.shiftActorPositionAndRestartAnimation();

                            //reset the clock time of every pawn in animation
                            if (actor.jobs.curDriver is rjw.JobDriver_Sex)
                            {
                                (actor.jobs.curDriver as rjw.JobDriver_Sex).ticks_left = def.animationTimeTicks;
                                (actor.jobs.curDriver as rjw.JobDriver_Sex).ticksLeftThisToil = def.animationTimeTicks;
                                (actor.jobs.curDriver as rjw.JobDriver_Sex).duration = def.animationTimeTicks;
                            }

                        }
*/
                    }

                    if (offsetX != ShowMeYourHandsModSettings.offsets[def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex].x || offsetZ != ShowMeYourHandsModSettings.offsets[def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex].y)
                    {
                        ShowMeYourHandsModSettings.offsets[def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex] = new Vector2(offsetX, offsetZ);

                    }

                    if (rotation != ShowMeYourHandsModSettings.rotation[def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex])
                    {
                        ShowMeYourHandsModSettings.rotation[def.defName + curPawn.def.defName + bodyTypeDef + ActorIndex] = rotation;
                    }

                }

            }
            else
            {
                listingStandard.Label("Select a pawn currently in an animation to change their offsets");
            }

            listingStandard.End();

        }

        public override void PreOpen()
        {
            base.PreOpen();
            if (ShowMeYourHandsModSettings.offsets == null)
            {
/*                if (AnimationSettings.debugMode)
                    Log.Message("New offsets");
*/                ShowMeYourHandsModSettings.offsets = new Dictionary<string, Vector2>();
            }

            if (ShowMeYourHandsModSettings.rotation == null)
            {
/*                if (AnimationSettings.debugMode)
                    Log.Message("New rotation");
*/                ShowMeYourHandsModSettings.rotation = new Dictionary<string, float>();
            }
        }

        public override void PostClose()
        {
            base.PostClose();

            LoadedModManager.GetMod<ShowMeYourHandsMod>().WriteSettings();
        }
    }
}
