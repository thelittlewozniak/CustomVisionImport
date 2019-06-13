using System.Collections.Generic;

namespace CustomVisionImport.Model
{
    public class Image
    {
        public string ResizedImageUri { get; set; }
        public List<TagImage> Tags { get; set; } = new List<TagImage>();
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<string> tagsIds { get; set; } = new List<string>();
        public string Url { get; set; }
    }
}
