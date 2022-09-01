using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class SoundDownloadStateItems
    {
        private const string SoundDownloadStateItemsFileName = "soundDownloadStateItems.json";

        public string Class { get; set; }
        public List<SoundDownloadItem> SoundItems { get; set; }

        public SoundDownloadStateItems(List<SoundDownloadItem> soundItems)
        {
            SoundItems = soundItems;

            if (soundItems.Count > 0)
                Class = SoundItems.First().GetType().Name;
            else
                Class = typeof(SoundDownloadItem).Name;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public async Task Save()
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile soundDownloadStateFile = await localCacheFolder.TryGetItemAsync(SoundDownloadStateItemsFileName) as StorageFile;

            if (soundDownloadStateFile == null)
                soundDownloadStateFile = await localCacheFolder.CreateFileAsync(SoundDownloadStateItemsFileName);

            await FileIO.WriteTextAsync(soundDownloadStateFile, ToJson());
        }

        public static async Task<SoundDownloadStateItems> Load()
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;

            StorageFile soundDownloadStateFile = await localCacheFolder.TryGetItemAsync(SoundDownloadStateItemsFileName) as StorageFile;
            if (soundDownloadStateFile == null) return null;

            string stateJson = await FileIO.ReadTextAsync(soundDownloadStateFile);
            if (stateJson == null) return null;

            return JsonSerializer.Deserialize<SoundDownloadStateItems>(stateJson);
        }

        public static async Task Delete()
        {
            StorageFolder localCacheFolder = ApplicationData.Current.LocalCacheFolder;
            StorageFile soundDownloadStateFile = await localCacheFolder.TryGetItemAsync(SoundDownloadStateItemsFileName) as StorageFile;

            if (soundDownloadStateFile == null)
                return;

            await soundDownloadStateFile.DeleteAsync();
        }
    }
}
