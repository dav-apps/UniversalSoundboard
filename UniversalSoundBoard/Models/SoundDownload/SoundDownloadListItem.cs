using System;
using System.Collections.Generic;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadListItem
    {
        public string Name { get; set; }
        public Uri Url { get; set; }
        public List<Uri> ImageUrls { get; set; }
        public bool Selected { get; set; }

        public SoundDownloadListItem(string name, Uri url, List<Uri> imageUrls, bool selected = false)
        {
            Name = name;
            Url = url;
            ImageUrls = imageUrls;
            Selected = selected;
        }

        public SoundDownloadListItem(string name, Uri url, Uri imageUrl, bool selected = false)
        {
            Name = name;
            Url = url;
            ImageUrls = new List<Uri> { imageUrl };
            Selected = selected;
        }
    }
}
