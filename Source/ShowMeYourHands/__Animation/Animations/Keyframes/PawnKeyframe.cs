using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimworld_Animations {
    public class PawnKeyframe : Keyframe
    {
        public float? bodyAngle;
        public float? headAngle;

        public float? genitalAngle;

        public float? bodyOffsetZ;
        public float? bodyOffsetX;

        public float? headBob;
        //todo: add headOffsets l/r?

        public int? bodyFacing;
        public int? headFacing;

        public bool? quiver;

    }
}
