namespace UniversalSoundboard.Models
{
    public class SoundDownloadListItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Length { get; set; }

        public SoundDownloadListItem(string name, string url, string length)
        {
            Name = name;
            Url = url;
            Length = length;
        }
    }
}
