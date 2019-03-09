using davClassLibrary.Common;
using System.Collections.Generic;

namespace UniversalSoundboard.Tests.Common
{
    class LocalDataSettings : ILocalDataSettings
    {
        private Dictionary<string, string> dataStore = new Dictionary<string, string>();

        public string GetValue(string key)
        {
            string value = null;
            dataStore.TryGetValue(key, out value);
            return value;
        }

        public void SetValue(string key, string value)
        {
            dataStore[key] = value;
        }
    }
}
