using System;
using System.IO;
using System.Linq;

namespace AnimationConverter
{
    public class Actor
    {
        public int id;
        public string head_id;
        public string body_id;

        public string handl_id;
        public string handr_id;
        public string footl_id;
        public string footr_id;

        public string headb_id;
        public string bodyb_id;
        public string handlb_id;
        public string handrb_id;
        public string footlb_id;
        public string footrb_id;

        public int GetFacing(Spriter spriterData, int folderId, int fileID)
        {
            int facing = 0;

            SpriterFile spriterFile = spriterData.Folders[folderId].Files[fileID];
            string fileName = Path.GetFileName(spriterFile.Name);

            facing = int.Parse(String.Join("", fileName.Where(char.IsDigit)));

            return facing;
        }
    }
}
