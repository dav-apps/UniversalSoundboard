﻿using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class SoundDetails
    {
        public string Category { get; set; }
        public bool Favourite { get; set; }

        public async Task ReadSoundDetailsFileAsync(StorageFile file)
        {
            // Read file
            string soundDetailsText = await FileIO.ReadTextAsync(file);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(SoundDetails));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(soundDetailsText));
            var data = (SoundDetails)serializer.ReadObject(ms);

            Category = data.Category;
            Favourite = data.Favourite;
        }
    }
}
