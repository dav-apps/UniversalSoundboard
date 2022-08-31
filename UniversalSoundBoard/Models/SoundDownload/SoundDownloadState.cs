using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadState
    {
        private const string SoundDownloadStateFileName = "soundDownloadState.json";

        public int CurrentIndex { get; set; }
        public Guid CategoryUuid { get; set; }

        public SoundDownloadState(int currentIndex, Guid categoryUuid)
        {
            CurrentIndex = currentIndex;
            CategoryUuid = categoryUuid;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public async Task Save()
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile soundDownloadStateFile = await localCacheFolder.TryGetItemAsync(SoundDownloadStateFileName) as StorageFile;

            if (soundDownloadStateFile == null)
                soundDownloadStateFile = await localCacheFolder.CreateFileAsync(SoundDownloadStateFileName);

            await FileIO.WriteTextAsync(soundDownloadStateFile, ToJson());
        }

        public static async Task<SoundDownloadState> Load()
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;

            StorageFile soundDownloadStateFile = await localCacheFolder.TryGetItemAsync(SoundDownloadStateFileName) as StorageFile;
            if (soundDownloadStateFile == null) return null;

            string stateJson = await FileIO.ReadTextAsync(soundDownloadStateFile);
            if (stateJson == null) return null;

            return JsonSerializer.Deserialize<SoundDownloadState>(stateJson);
        }
    }
}
