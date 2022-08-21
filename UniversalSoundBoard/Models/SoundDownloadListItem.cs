using System;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadListItem
    {
        public string Name { get; set; }
        public Uri Url { get; set; }
        public Uri ImageUrl { get; set; }

        public SoundDownloadListItem(string name, Uri url, Uri imageUrl)
        {
            Name = name;
            Url = url;
            ImageUrl = imageUrl;
        }
    }
}
