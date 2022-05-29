using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Rimworld_Animations {
    public class Actor {
        public List<string> defNames;
        public List<string> requiredGenitals;
        public List<AlienRaceOffset> raceOffsets;
        public List<string> blacklistedRaces;
        public bool initiator = false;
        public string gender;
        public bool isFucking = false;
        public bool isFucked = false;
        public bool controlGenitalAngle = false;
        public List<BodyDef> bodyDefTypes = new List<BodyDef>();
        public BodyTypeOffset bodyTypeOffset = new BodyTypeOffset();
        public Vector3 offset = new Vector2(0, 0);
		public List<string> requiredGender;

    }
}
