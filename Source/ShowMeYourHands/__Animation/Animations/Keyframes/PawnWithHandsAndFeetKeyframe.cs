using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Rimworld_Animations {
    public class PawnWithHandsAndFeetKeyframe : Keyframe
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

        public float? handlAngle;
        public float? handrAngle;
        public float? footlAngle;
        public float? footrAngle;
       
        public float? handlOffsetX;
        public float? handrOffsetX;
        public float? footlOffsetX;
        public float? footrOffsetX;

        public float? handlOffsetZ;
        public float? handrOffsetZ;
        public float? footlOffsetZ;
        public float? footrOffsetZ;

    }
}
