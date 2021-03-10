using davClassLibrary.Common;
using Windows.Storage;

namespace UniversalSoundboard.Common
{
    public class LocalDataSettings : ILocalDataSettings
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private const string davKey = "dav";

        public void Set(string key, string value)
        {
            SetObject(key, value);
        }

        public void Set(string key, int value)
        {
            SetObject(key, value);
        }

        public void Set(string key, long value)
        {
            SetObject(key, value);
        }

        public string GetString(string key)
        {
            var value = GetDavComposite()[key];
            return value != null ? (string)value : null;
        }

        public int GetInt(string key)
        {
            var value = GetDavComposite()[key];
            return value != null ? (int)value : 0;
        }

        public long GetLong(string key)
        {
            var value = GetDavComposite()[key];
            return value != null ? (long)value : 0;
        }

        public void Remove(string key)
        {
            GetDavComposite().Remove(key);
        }

        private void SetObject(string key, object value)
        {
            var davComposite = GetDavComposite();
            davComposite[key] = value;
            SetDavComposite(davComposite);
        }

        private ApplicationDataCompositeValue GetDavComposite()
        {
            // Save all values of davClassLibrary in a separate composite
            var composite = (ApplicationDataCompositeValue)localSettings.Values[davKey];
            return composite ?? new ApplicationDataCompositeValue();
        }

        private void SetDavComposite(ApplicationDataCompositeValue composite)
        {
            localSettings.Values[davKey] = composite;
        }
    }
}
