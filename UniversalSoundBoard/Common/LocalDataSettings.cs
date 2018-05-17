using davClassLibrary.Common;
using UniversalSoundBoard.DataAccess;
using Windows.Storage;

namespace UniversalSoundboard.Common
{
    public class LocalDataSettings : ILocalDataSettings
    {
        private ApplicationDataCompositeValue davComposite = (ApplicationDataCompositeValue)ApplicationData.Current.LocalSettings.Values[FileManager.davKey];

        public string GetValue(string key)
        {
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

        public void SaveValue(string key, string value)
        {
            // Save all values of the davClassLibrary in a separate composite
            davComposite[key] = value;
        }
    }
}
