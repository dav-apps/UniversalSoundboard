using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace UniversalSoundboard.Models
{
    public class RecordedSoundItem
    {
        public string Name { get; set; }
        public StorageFile File { get; set; }

        public RecordedSoundItem(string name, StorageFile file)
        {
            Name = name;
            File = file;
        }
    }
}
