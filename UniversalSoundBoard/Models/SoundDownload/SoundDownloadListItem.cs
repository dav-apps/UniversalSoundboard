using System;
using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadListItem
    {
        public string Name { get; set; }
        public Uri Url { get; set; }
        public List<Uri> ImageUrls { get; set; }

        public SoundDownloadListItem(string name, Uri url, List<Uri> imageUrls)
        {
            Name = name;
            Url = url;
            ImageUrls = imageUrls;
        }

        public SoundDownloadListItem(string name, Uri url, Uri imageUrl)
        {
            Name = name;
            Url = url;
            ImageUrls = new List<Uri> { imageUrl };
        }
    }
}
