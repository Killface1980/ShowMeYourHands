using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace AnimationConverter
{
    [XmlRoot("Rimworld_Animations.AnimationDef")]
    public class AnimationDef
    {
        [XmlArray("animationStages"), XmlArrayItem("li")]
        public List<AnimationStage> animationStages = new List<AnimationStage>();
    }

    public class AnimationStage
    {
        public string stageName = "default";
        public int stageIndex = 0;
        public int playTimeTicks = 0;
        public bool isLooping = true;

        [XmlArray("animationClips"), XmlArrayItem("li")]
        public List<PawnAnimationClip> animationClips = new List<PawnAnimationClip>();
    }

    public class PawnAnimationClip
    {
        [XmlAttribute("Class")]
        public string className = "Rimworld_Animations.PawnAnimationClip";

        public string layer = "Pawn";

        [XmlArray("keyframes"), XmlArrayItem("li")]
        public List<KeyFrame> keyframes = new List<KeyFrame>();
    }

    public class KeyFrame
    {
        public float bodyAngle;
        public float handlAngle;
        public float handrAngle;
        public float footlAngle;
        public float footrAngle;
        public float headAngle;
        public float headBob;
        public float bodyOffsetX;
        public float bodyOffsetZ;
        public float handlOffsetX;
        public float handlOffsetZ;
        public float handrOffsetX;
        public float handrOffsetZ;
        public float footlOffsetX;
        public float footlOffsetZ;
        public float footrOffsetX;
        public float footrOffsetZ;
        public float headFacing;
        public float bodyFacing;
        public float handlFacing;
        public float handrFacing;
        public float footlFacing;
        public float footrFacing;
        public int tickDuration;
    }

}
