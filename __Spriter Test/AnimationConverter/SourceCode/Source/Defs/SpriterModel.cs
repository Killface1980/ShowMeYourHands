using System.Collections.Generic;
using System.Xml.Serialization;

namespace AnimationConverter
{
    [XmlRoot("spriter_data")]
    public class Spriter
    {
        [XmlElement("folder")]
        public List<SpriterFolder> Folders = new List<SpriterFolder>();

        [XmlElement("entity")]
        public List<SpriterEntity> Entities = new List<SpriterEntity>();

        [XmlArray("tag_list"), XmlArrayItem("i")]
        public List<SpriterElement> Tags = new List<SpriterElement>();

        [XmlArray("atlas"), XmlArrayItem("i")]
        public List<SpriterElement> Atlases = new List<SpriterElement>();
    }

    public class SpriterFolder : SpriterElement
    {
        [XmlElement("file")]
        public List<SpriterFile> Files = new List<SpriterFile>();

        [XmlAttribute("atlas")]
        public int AtlasId;

        public SpriterFolder()
        {
            this.AtlasId = -1;
        }
    }

    public class SpriterFile : SpriterElement
    {
        [XmlAttribute("type")]
        public SpriterFileType Type;

        [XmlAttribute("pivot_x")]
        public float PivotX;

        [XmlAttribute("pivot_y")]
        public float PivotY;

        [XmlAttribute("width")]
        public int Width;

        [XmlAttribute("height")]
        public int Height;

        public SpriterFile()
        {
            this.PivotX = 0f;
            this.PivotY = 1f;
        }
    }

    public class SpriterEntity : SpriterElement
    {
        public Spriter Spriter;

        [XmlElement("obj_info")]
        public List<SpriterObjectInfo> ObjectInfos = new List<SpriterObjectInfo>();

        [XmlElement("character_map")]
        public List<SpriterCharacterMap> CharacterMaps = new List<SpriterCharacterMap>();

        [XmlElement("animation")]
        public List<SpriterAnimation> Animations = new List<SpriterAnimation>();

        [XmlArray("var_defs"), XmlArrayItem("i")]
        public List<SpriterVarDef> Variables = new List<SpriterVarDef>();
    }

    public class SpriterObjectInfo : SpriterElement
    {
        [XmlAttribute("type")]
        public SpriterObjectType ObjectType;

        [XmlAttribute("w")]
        public float Width;

        [XmlAttribute("h")]
        public float Height;

        [XmlAttribute("pivot_x")]
        public float PivotX;

        [XmlAttribute("pivot_y")]
        public float PivotY;

        [XmlArray("var_defs"), XmlArrayItem("i")]
        public List<SpriterVarDef> Variables = new List<SpriterVarDef>();
    }

    public class SpriterAnimation : SpriterElement
    {
        public SpriterEntity Entity;

        [XmlAttribute("length")]
        public float Length;

        [XmlAttribute("looping")]
        public bool Looping;

        [XmlArray("mainline"), XmlArrayItem("key")]
        public List<SpriterMainlineKey> MainlineKeys = new List<SpriterMainlineKey>();

        [XmlElement("timeline")]
        public List<SpriterTimeline> Timelines = new List<SpriterTimeline>();

        [XmlElement("eventline")]
        public List<SpriterEventline> Eventlines = new List<SpriterEventline>();

        [XmlElement("soundline")]
        public List<SpriterSoundline> Soundlines = new List<SpriterSoundline>();

        [XmlElement("meta")]
        public SpriterMeta Meta;

        public SpriterAnimation()
        {
            this.Looping = true;
        }
    }

    public class SpriterMainlineKey : SpriterKey
    {
        [XmlElement("bone_ref")]
        public List<SpriterRef> BoneRefs = new List<SpriterRef>();

        [XmlElement("object_ref")]
        public List<SpriterObjectRef> ObjectRefs = new List<SpriterObjectRef>();
    }

    public class SpriterRef : SpriterElement
    {
        [XmlAttribute("parent")]
        public int ParentId;

        [XmlAttribute("timeline")]
        public int TimelineId;

        [XmlAttribute("key")]
        public int KeyId;

        public SpriterRef()
        {
            this.ParentId = -1;
        }
    }

    public class SpriterObjectRef : SpriterRef
    {
        [XmlAttribute("z_index")]
        public int ZIndex;
    }

    public class SpriterTimeline : SpriterElement
    {
        [XmlAttribute("object_type")]
        public SpriterObjectType ObjectType;

        [XmlAttribute("obj")]
        public int ObjectId;

        [XmlElement("key")]
        public List<SpriterTimelineKey> Keys = new List<SpriterTimelineKey>();

        [XmlElement("meta")]
        public SpriterMeta Meta;
    }

    public class SpriterTimelineKey : SpriterKey
    {
        [XmlAttribute("spin")]
        public int Spin;

        [XmlElement("bone", typeof(SpriterSpatial))]
        public SpriterSpatial BoneInfo;

        [XmlElement("object", typeof(SpriterObject))]
        public SpriterObject ObjectInfo;

        public SpriterTimelineKey()
        {
            this.Spin = 1;
        }
    }

    public class SpriterSpatial
    {
        [XmlAttribute("x")]
        public float X;

        [XmlAttribute("y")]
        public float Y;

        [XmlAttribute("angle")]
        public float Angle;

        [XmlAttribute("scale_x")]
        public float ScaleX;

        [XmlAttribute("scale_y")]
        public float ScaleY;

        [XmlAttribute("a")]
        public float Alpha;

        public SpriterSpatial()
        {
            this.ScaleX = 1;
            this.ScaleY = 1;
            this.Alpha = 1;
        }
    }

    public class SpriterObject : SpriterSpatial
    {
        [XmlAttribute("animation")]
        public int AnimationId;

        [XmlAttribute("entity")]
        public int EntityId;

        [XmlAttribute("folder")]
        public int FolderId;

        [XmlAttribute("file")]
        public int FileId;

        [XmlAttribute("pivot_x")]
        public float PivotX;

        [XmlAttribute("pivot_y")]
        public float PivotY;

        [XmlAttribute("t")]
        public float T;

        public SpriterObject()
        {
            this.PivotX = float.NaN;
            this.PivotY = float.NaN;
        }
    }

    public class SpriterCharacterMap : SpriterElement
    {
        [XmlElement("map")]
        public List<SpriterMapInstruction> Maps = new List<SpriterMapInstruction>();
    }

    public class SpriterMapInstruction
    {
        [XmlAttribute("folder")]
        public int FolderId;

        [XmlAttribute("file")]
        public int FileId;

        [XmlAttribute("target_folder")]
        public int TargetFolderId;

        [XmlAttribute("target_file")]
        public int TargetFileId;

        public SpriterMapInstruction()
        {
            this.TargetFolderId = -1;
            this.TargetFileId = -1;
        }
    }

    public class SpriterMeta
    {
        [XmlElement("varline")]
        public List<SpriterVarline> Varlines = new List<SpriterVarline>();

        [XmlElement("tagline")]
        public SpriterTagline Tagline;
    }

    public class SpriterVarDef : SpriterElement
    {
        [XmlAttribute("type")]
        public SpriterVarType Type;

        [XmlAttribute("default")]
        public string DefaultValue;

        [XmlIgnore]
        public SpriterVarValue VariableValue;
    }

    public class SpriterVarline : SpriterElement
    {
        [XmlAttribute("def")]
        public int Def;

        [XmlElement("key")]
        public List<SpriterVarlineKey> Keys = new List<SpriterVarlineKey>();
    }

    public class SpriterVarlineKey : SpriterKey
    {
        [XmlAttribute("val")]
        public string Value;

        [XmlIgnore]
        public SpriterVarValue VariableValue;
    }

    public class SpriterVarValue
    {
        public SpriterVarType Type;
        public string StringValue;
        public float FloatValue;
        public int IntValue;
    }

    public class SpriterEventline : SpriterElement
    {
        [XmlElement("key")]
        public List<SpriterKey> Keys = new List<SpriterKey>();
    }

    public class SpriterTagline
    {
        [XmlElement("key")]
        public List<SpriterTaglineKey> Keys = new List<SpriterTaglineKey>();
    }

    public class SpriterTaglineKey : SpriterKey
    {
        [XmlElement("tag")]
        public List<SpriterTag> Tags = new List<SpriterTag>();
    }

    public class SpriterTag : SpriterElement
    {
        [XmlAttribute("t")]
        public int TagId;
    }

    public class SpriterSoundline : SpriterElement
    {
        [XmlElement("key")]
        public List<SpriterSoundlineKey> Keys = new List<SpriterSoundlineKey>();
    }

    public class SpriterSoundlineKey : SpriterKey
    {
        [XmlElement("object")]
        public SpriterSound SoundObject;
    }

    public class SpriterSound : SpriterElement
    {
        [XmlAttribute("folder")]
        public int FolderId;

        [XmlAttribute("file")]
        public int FileId;

        [XmlAttribute("trigger")]
        public bool Trigger;

        [XmlAttribute("panning")]
        public float Panning;

        [XmlAttribute("volume")]
        public float Volume;

        public SpriterSound()
        {
            this.Trigger = true;
            this.Volume = 1.0f;
        }
    }

    public class SpriterElement
    {
        [XmlAttribute("id")]
        public int Id;

        [XmlAttribute("name")]
        public string Name;
    }

    public class SpriterKey : SpriterElement
    {
        [XmlAttribute("time")]
        public float Time;

        [XmlAttribute("curve_type")]
        public SpriterCurveType CurveType;

        [XmlAttribute("c1")]
        public float C1;

        [XmlAttribute("c2")]
        public float C2;

        [XmlAttribute("c3")]
        public float C3;

        [XmlAttribute("c4")]
        public float C4;

        public SpriterKey()
        {
            this.Time = 0;
        }
    }

    public enum SpriterObjectType
    {
        [XmlEnum("sprite")]
        Sprite,

        [XmlEnum("bone")]
        Bone,

        [XmlEnum("box")]
        Box,

        [XmlEnum("point")]
        Point,

        [XmlEnum("sound")]
        Sound,

        [XmlEnum("entity")]
        Entity,

        [XmlEnum("variable")]
        Variable
    }

    public enum SpriterCurveType
    {
        [XmlEnum("linear")]
        Linear,

        [XmlEnum("instant")]
        Instant,

        [XmlEnum("quadratic")]
        Quadratic,

        [XmlEnum("cubic")]
        Cubic,

        [XmlEnum("quartic")]
        Quartic,

        [XmlEnum("quintic")]
        Quintic,

        [XmlEnum("bezier")]
        Bezier
    }

    public enum SpriterFileType
    {
        Image,

        [XmlEnum("sound")]
        Sound
    }

    public enum SpriterVarType
    {
        [XmlEnum("string")]
        String,

        [XmlEnum("int")]
        Int,

        [XmlEnum("float")]
        Float
    }
}