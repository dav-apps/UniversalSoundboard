using System;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundboard.Models
{
    public class User
    {
        public string Username { get; set; }
        public BitmapImage Avatar { get; set; }
        public long TotalStorage { get; set; }
        public long UsedStorage { get; set; }

        public User()
        {

        }

        public User(string username, long totalStorage, long usedStorage)
        {

        }
    }
}
