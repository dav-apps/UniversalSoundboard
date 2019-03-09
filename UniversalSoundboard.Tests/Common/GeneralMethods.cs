using davClassLibrary.Common;
using davClassLibrary.DataAccess;

namespace UniversalSoundboard.Tests.Common
{
    class GeneralMethods : IGeneralMethods
    {
        public DavEnvironment GetEnvironment()
        {
            return DavEnvironment.Test;
        }

        public bool IsNetworkAvailable()
        {
            return true;
        }
    }
}
