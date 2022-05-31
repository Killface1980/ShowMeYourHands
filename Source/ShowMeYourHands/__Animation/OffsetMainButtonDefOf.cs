using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rimworld_Animations
{

    [DefOf]
    public static class OffsetMainButtonDefOf
    {

        public static MainButtonDef OffsetManager;


        static OffsetMainButtonDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(OffsetMainButtonDefOf));
        }

    }
}
