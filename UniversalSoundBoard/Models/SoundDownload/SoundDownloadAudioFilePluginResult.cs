namespace UniversalSoundboard.Models
{
    public class SoundDownloadAudioFilePluginResult : SoundDownloadPluginResult
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }

        public SoundDownloadAudioFilePluginResult(string fileName, string fileType, long fileSize)
        {
            FileName = fileName;
            FileType = fileType;
            FileSize = fileSize;
        }
    }
}
