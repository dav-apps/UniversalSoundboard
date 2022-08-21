namespace UniversalSoundboard.Models
{
    public class SoundDownloadListItem
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public SoundDownloadListItem(string name, string url)
        {
            Name = name;
            Url = url;
        }
    }
}
