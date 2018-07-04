using davClassLibrary.Common;
using UniversalSoundBoard.DataAccess;
using Windows.Storage;

namespace UniversalSoundboard.Common
{
    public class LocalDataSettings : ILocalDataSettings
    {
        private ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public string GetValue(string key)
        {
            var davComposite = GetComposite();

            // Save all values of the davClassLibrary in a separate composite
            if (davComposite != null)
            {
                var value = davComposite[key];

                if (value != null)
                {
                    return value.ToString();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
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
            var composite = (ApplicationDataCompositeValue)localSettings.Values[FileManager.davKey];
            if(composite == null)
            {
                return new ApplicationDataCompositeValue();
            }
            else
            {
                return composite;
            }
        }

        private void SetComposite(ApplicationDataCompositeValue composite)
        {
            localSettings.Values[FileManager.davKey] = composite;
        }
    }
}
