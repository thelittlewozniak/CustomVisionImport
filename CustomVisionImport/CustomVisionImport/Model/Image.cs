using System.Collections.Generic;

namespace CustomVisionImport.Model
{
    public class Image
    {
        public string ResizedImageUri { get; set; }
        public List<TagImage> Tags { get; set; }
        public List<Region> Regions { get; set; }
        public List<string> tagsIds { get; set; }
        public string Url { get; set; }
    }
}
