using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace UniversalSoundBoard.Model
{
    public class SoundDetails
    {
        public string Category { get; set; }

        public async Task ReadSoundDetailsFile(StorageFile file)
        {
            // Read file
            string soundDetailsText = await FileIO.ReadTextAsync(file);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(SoundDetails));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(soundDetailsText));
            var data = (SoundDetails)serializer.ReadObject(ms);

            this.Category = data.Category;
        }
    }
}
