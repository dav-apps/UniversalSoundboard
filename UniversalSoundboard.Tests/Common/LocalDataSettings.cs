using davClassLibrary.Common;
using System.Collections.Generic;

namespace UniversalSoundboard.Tests.Common
{
    class LocalDataSettings : ILocalDataSettings
    {
        private readonly Dictionary<string, object> dataStore = new Dictionary<string, object>();
        
        public void Set(string key, string value)
        {
            dataStore[key] = value;
        }

        public void Set(string key, int value)
        {
            dataStore[key] = value;
        }

        public void Set(string key, long value)
        {
            dataStore[key] = value;
        }

        public string GetString(string key)
        {
            if (dataStore.ContainsKey(key))
                return (string)dataStore[key];
            return null;
        }

        public int GetInt(string key)
        {
            if (dataStore.ContainsKey(key))
                return (int)dataStore[key];
            return 0;
        }

        public long GetLong(string key)
        {
            if (dataStore.ContainsKey(key))
                return (long)dataStore[key];
            return 0;
        }

        public void Remove(string key)
        {
            dataStore.Remove(key);
        }
    }
}
