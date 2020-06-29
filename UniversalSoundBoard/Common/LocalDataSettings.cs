using davClassLibrary.Common;
using UniversalSoundBoard.DataAccess;
using Windows.Storage;

namespace UniversalSoundboard.Common
{
    public class LocalDataSettings : ILocalDataSettings
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        private const string davKey = "dav";

        public string GetValue(string key)
        {
            var davComposite = GetComposite();

            // Save all values of the davClassLibrary in a separate composite
            if (davComposite != null)
            {
                var value = davComposite[key];

                if (value != null)
                    return value.ToString();
                else
                    return null;
            }
            else
                return null;
        }

        public void SetValue(string key, string value)
        {
            // Save all values of the davClassLibrary in a separate composite
            var davComposite = GetComposite();
            davComposite[key] = value;
            SetComposite(davComposite);
        }

        private ApplicationDataCompositeValue GetComposite()
        {
            var composite = (ApplicationDataCompositeValue)localSettings.Values[davKey];
            return composite ?? new ApplicationDataCompositeValue();
        }

        private void SetComposite(ApplicationDataCompositeValue composite)
        {
            localSettings.Values[davKey] = composite;
        }
    }
}
