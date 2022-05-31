using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using ShowMeYourHands;
using Verse;

namespace Rimworld_Animations
{
    public class WorldComponent_UpdateMainTab : WorldComponent
    {

        public WorldComponent_UpdateMainTab(World world) : base(world)
        {

        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            OffsetMainButtonDefOf.OffsetManager.buttonVisible = ShowMeYourHandsModSettings.offsetTab;
        }


    }
}
