using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.Xml.Serialization;

namespace AnimationConverter
{
    public class XMLParsing
    {
        private Spriter spriterData = new Spriter();
        private AnimationDef animationDef = new AnimationDef();
        
        private TextBox ouputTextbox;
        private int scaleXZ = 96;
        private bool scaleTiming = false;

        public void BeginParsing(string inputPath, TextBox ouputTextbox, int scaleXZ, bool scaleTiming)
        {
            this.scaleXZ = scaleXZ;
            this.scaleTiming = scaleTiming;
            this.ouputTextbox = ouputTextbox;

            this.spriterData = this.ReadDataFromSCML<Spriter>(inputPath);
            this.ExtractRevelantData();
            this.WriteDataToXML();
        }
        private T ReadDataFromSCML<T>(string inputPath)
        {
            using (StreamReader stringReader = new StreamReader(inputPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(stringReader);
            }
        }

        private void ExtractRevelantData()
        {
            // Get animation data
            List<SpriterAnimation> spriterAnimations = this.spriterData.Entities[0].Animations;

            // Work out the number of animations, actors per animationa and keyframes per animation
            int numAnimationStages = spriterAnimations.Count;
            List<int> numActors = new List<int>();
            Dictionary<int, Dictionary<string, Actor>> actorsInAnimationStages = new Dictionary<int, Dictionary<string, Actor>>();
            List<int> numKeyframes = new List<int>();

            for (int i = 0; i < spriterAnimations.Count; i++)
            {
                SpriterAnimation spriterAnimation = spriterAnimations[i];

                Dictionary<string, Actor> actors = new Dictionary<string, Actor>();

                foreach (SpriterTimeline spriterTimeline in spriterAnimations[0].Timelines)
                {
                    if (!spriterTimeline.Name.Contains("_head") && !spriterTimeline.Name.Contains("_body") && !spriterTimeline.Name.Contains("_handl") && !spriterTimeline.Name.Contains("_handr") &&!spriterTimeline.Name.Contains("_footl") && !spriterTimeline.Name.Contains("_footr"))
                    { continue; }

                    string id = spriterTimeline.Name.Substring(0, spriterTimeline.Name.LastIndexOf("_"));
                    Actor actor;

                    if (!actors.Keys.Contains(id))
                    {
                        actor = new Actor();
                        actor.id = actors.Count;
                        actors.Add(id, actor);
                    }

                    actor = actors[id];

                    if (spriterTimeline.Name.EndsWith("_head"))
                    { actor.head_id = spriterTimeline.Name; }

                    if (spriterTimeline.Name.EndsWith("_body"))
                    { actor.body_id = spriterTimeline.Name; }
                 
                    if (spriterTimeline.Name.EndsWith("_handl"))
                    { actor.handl_id = spriterTimeline.Name; }
                    
                    if (spriterTimeline.Name.EndsWith("_handr"))
                    { actor.handr_id = spriterTimeline.Name; }
                    
                    if (spriterTimeline.Name.EndsWith("_footl"))
                    { actor.footl_id = spriterTimeline.Name; }
                    
                    if (spriterTimeline.Name.EndsWith("_footr"))
                    { actor.footr_id = spriterTimeline.Name; }
                    // Bones

                    if (spriterTimeline.Name.EndsWith("_headb"))
                    { actor.headb_id = spriterTimeline.Name; }

                    if (spriterTimeline.Name.EndsWith("_bodyb"))
                    { actor.bodyb_id = spriterTimeline.Name; }

                    if (spriterTimeline.Name.EndsWith("_handlb"))
                    { actor.handlb_id = spriterTimeline.Name; }

                    if (spriterTimeline.Name.EndsWith("_handrb"))
                    { actor.handrb_id = spriterTimeline.Name; }

                    if (spriterTimeline.Name.EndsWith("_footlb"))
                    { actor.footlb_id = spriterTimeline.Name; }

                    if (spriterTimeline.Name.EndsWith("_footrb"))
                    { actor.footrb_id = spriterTimeline.Name; }

                }

                numActors.Add(actors.Count);
                actorsInAnimationStages.Add(i, actors);
                numKeyframes.Add(spriterAnimation.MainlineKeys.Count);
            }

            // Set up a skeleton array to be populated
            for (int i = 0; i < numAnimationStages; i++)
            {
                this.animationDef.animationStages.Add(new AnimationStage());

                for (int j = 0; j < numActors[i]; j++)
                {
                    this.animationDef.animationStages[i].animationClips.Add(new PawnAnimationClip());

                    for (int k = 0; k < numKeyframes[i]; k++)
                    {
                        this.animationDef.animationStages[i].animationClips[j].keyframes.Add(new KeyFrame());
                    }
                }
            }

            // Fill in the skeleton array
            for (int i = 0; i < numAnimationStages; i++)
            {
                this.animationDef.animationStages[i].stageName = spriterAnimations[i].Name;
                this.animationDef.animationStages[i].playTimeTicks = (int)(spriterAnimations[i].Length / (this.scaleTiming ? 10 : 1));

                Dictionary<string, Actor> actorsInCurrentStage = actorsInAnimationStages[i];
                bool lastKeyFrameLengthZero = false;

                foreach (KeyValuePair<string,Actor> actor in actorsInCurrentStage)
                {
                    SpriterTimeline headTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.head_id);
                    SpriterTimeline bodyTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.body_id);

                    SpriterTimeline handlTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.handl_id);
                    SpriterTimeline handrTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.handr_id);
                    SpriterTimeline footlTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.footl_id);
                    SpriterTimeline footrTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.footr_id);

                    SpriterTimeline headbTimeline  = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.headb_id);
                    SpriterTimeline bodybTimeline  = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.bodyb_id);
                    SpriterTimeline handlbTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.handlb_id);
                    SpriterTimeline handrbTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.handrb_id);
                    SpriterTimeline footlbTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.footlb_id);
                    SpriterTimeline footrbTimeline = spriterAnimations[i].Timelines.Single(x => x.Name == actor.Value.footrb_id);

                    Actor actorData = actor.Value;

                    for (int k = 0; k < numKeyframes[i]; k++)
                    {
                        List<SpriterObjectRef> keyDataObjectRefs = this.spriterData.Entities[0].Animations[i].MainlineKeys[k].ObjectRefs;
                        List<SpriterRef> keyDataBoneRefs = this.spriterData.Entities[0].Animations[i].MainlineKeys[k].BoneRefs;

                        int headDataKey  = keyDataObjectRefs.Single(x => x.TimelineId == headTimeline.Id).KeyId;
                        int bodyDataKey  = keyDataObjectRefs.Single(x => x.TimelineId == bodyTimeline.Id).KeyId;
                        int handlDataKey = keyDataObjectRefs.Single(x => x.TimelineId == handlTimeline.Id).KeyId;
                        int handrDataKey = keyDataObjectRefs.Single(x => x.TimelineId == handrTimeline.Id).KeyId;
                        int footlDataKey = keyDataObjectRefs.Single(x => x.TimelineId == footlTimeline.Id).KeyId;
                        int footrDataKey = keyDataObjectRefs.Single(x => x.TimelineId == footrTimeline.Id).KeyId;


                        SpriterObject headData = headTimeline.Keys[headDataKey].ObjectInfo;
                        SpriterObject bodyData = bodyTimeline.Keys[bodyDataKey].ObjectInfo;
                        SpriterObject handlData = handlTimeline.Keys[handlDataKey].ObjectInfo;
                        SpriterObject handrData = handrTimeline.Keys[handrDataKey].ObjectInfo;
                        SpriterObject footlData = footlTimeline.Keys[footlDataKey].ObjectInfo;
                        SpriterObject footrData = footrTimeline.Keys[footrDataKey].ObjectInfo;


                        int headbDataKey  = keyDataBoneRefs.Single(x => x.TimelineId == headbTimeline.Id).KeyId;
                        int bodybDataKey  = keyDataBoneRefs.Single(x => x.TimelineId == bodybTimeline.Id).KeyId;
                        int handlbDataKey = keyDataBoneRefs.Single(x => x.TimelineId == handlbTimeline.Id).KeyId;
                        int handrbDataKey = keyDataBoneRefs.Single(x => x.TimelineId == handrbTimeline.Id).KeyId;
                        int footlbDataKey = keyDataBoneRefs.Single(x => x.TimelineId == footlbTimeline.Id).KeyId;
                        int footrbDataKey = keyDataBoneRefs.Single(x => x.TimelineId == footrbTimeline.Id).KeyId;
                        // 
                        // 
                        SpriterSpatial headbData  = headbTimeline.Keys[headbDataKey].BoneInfo;
                        SpriterSpatial bodybData  = bodybTimeline.Keys[bodybDataKey].BoneInfo;
                        SpriterSpatial handlbData = handlbTimeline.Keys[handlbDataKey].BoneInfo;
                        SpriterSpatial handrbData = handrbTimeline.Keys[handrbDataKey].BoneInfo;
                        SpriterSpatial footlbData = footlbTimeline.Keys[footlbDataKey].BoneInfo;
                        SpriterSpatial footrbData = footrbTimeline.Keys[footrbDataKey].BoneInfo;


                        // Get head facing
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].headFacing = actorData.GetFacing(this.spriterData, headData.FolderId, headData.FileId);

                        // Get body facing
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].bodyFacing = actorData.GetFacing(this.spriterData, bodyData.FolderId, bodyData.FileId);

                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handlFacing = actorData.GetFacing(this.spriterData, handlData.FolderId, handlData.FileId);
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handrFacing = actorData.GetFacing(this.spriterData, handrData.FolderId, handrData.FileId);
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footlFacing = actorData.GetFacing(this.spriterData, footlData.FolderId, footlData.FileId);
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footrFacing = actorData.GetFacing(this.spriterData, footrData.FolderId, footrData.FileId);


                        // Get head angle
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].headAngle = (headData.Angle <= 180 ? headData.Angle : headData.Angle - 360) * -1;

                        // Get body angle
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].bodyAngle = (bodyData.Angle <= 180 ? bodyData.Angle : bodyData.Angle - 360) * -1;

                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handlAngle = (handlData.Angle <= 180 ? handlData.Angle : handlData.Angle - 360) * -1;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handrAngle = (handrData.Angle <= 180 ? handrData.Angle : handrData.Angle - 360) * -1;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footlAngle = (footlData.Angle <= 180 ? footlData.Angle : footlData.Angle - 360) * -1;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footrAngle = (footrData.Angle <= 180 ? footrData.Angle : footrData.Angle - 360) * -1;

                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].headAngle += (headbData.Angle <= 180 ? headbData.Angle : headbData.Angle - 360) * -1;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].bodyAngle += (bodybData.Angle <= 180 ? bodybData.Angle : bodybData.Angle - 360) * -1;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handlAngle += (handlbData.Angle <= 180 ? handlbData.Angle : handlbData.Angle - 360) * -1;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handrAngle += (handrbData.Angle <= 180 ? handrbData.Angle : handrbData.Angle - 360) * -1;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footlAngle += (footlbData.Angle <= 180 ? footlbData.Angle : footlbData.Angle - 360) * -1;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footrAngle += (footrbData.Angle <= 180 ? footrbData.Angle : footrbData.Angle - 360) * -1;


                        // Get body postion
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].bodyOffsetX = bodyData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].bodyOffsetZ = bodyData.Y / this.scaleXZ;

                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handlOffsetX = handlData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handlOffsetZ = handlData.Y / this.scaleXZ;

                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handrOffsetX = handrData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handrOffsetZ = handrData.Y / this.scaleXZ;

                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footlOffsetX = footlData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footlOffsetZ = footlData.Y / this.scaleXZ;

                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footrOffsetX = footrData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footrOffsetZ = footrData.Y / this.scaleXZ;

                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].bodyOffsetX += bodybData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].bodyOffsetZ += bodybData.Y / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handlOffsetX += handlbData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handlOffsetZ += handlbData.Y / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handrOffsetX += handrbData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].handrOffsetZ += handrbData.Y / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footlOffsetX += footlbData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footlOffsetZ += footlbData.Y / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footrOffsetX += footrbData.X / this.scaleXZ;
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].footrOffsetZ += footrbData.Y / this.scaleXZ;

                        // Tick duration
                        int currentTime = (int)(this.spriterData.Entities[0].Animations[i].MainlineKeys[k].Time / (this.scaleTiming ? 10 : 1));

                        if (k + 1 < numKeyframes[i])
                        {
                            int nextTime = (int)(this.spriterData.Entities[0].Animations[i].MainlineKeys[k + 1].Time / (this.scaleTiming ? 10 : 1));
                            this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].tickDuration = nextTime - currentTime; 
                        }

                        else if (currentTime < this.animationDef.animationStages[i].playTimeTicks)
                        {
                            this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].tickDuration = this.animationDef.animationStages[i].playTimeTicks - currentTime; 
                        }
                        
                        else
                        {
                            this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].tickDuration = 1;
                            lastKeyFrameLengthZero = true;
                        }

                        // Head bob
                        var bodyBX = bodyData.X + bodybData.X;
                        var bodyBY = bodyData.Y + bodybData.Y;
                        double bob = Math.Sqrt(Math.Pow((headData.X / this.scaleXZ - bodyBX / this.scaleXZ), 2) + Math.Pow((headData.Y / this.scaleXZ - bodyBY / this.scaleXZ), 2));
                        this.animationDef.animationStages[i].animationClips[actorData.id].keyframes[k].headBob = (float)(bob);
                    }
                }

                if (lastKeyFrameLengthZero)
                { this.animationDef.animationStages[i].playTimeTicks++; }
            }
        }

        private void WriteDataToXML()
        {
            using (StringWriter stringWriter = new StringWriter(new StringBuilder()))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(AnimationDef));
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                xmlSerializer.Serialize(stringWriter, this.animationDef, namespaces);
                this.ouputTextbox.Text = stringWriter.ToString();
            }
        }
    }
}
