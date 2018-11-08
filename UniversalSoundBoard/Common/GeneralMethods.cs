using davClassLibrary.Common;
using davClassLibrary.DataAccess;
using UniversalSoundBoard.DataAccess;
using Windows.Networking.Connectivity;

namespace UniversalSoundboard.Common
{
    public class GeneralMethods : IGeneralMethods
    {
        public bool IsNetworkAvailable()
        {
            var connection = NetworkInformation.GetInternetConnectionProfile();
            var networkCostType = connection.GetConnectionCost().NetworkCostType;
            return !(networkCostType != NetworkCostType.Unrestricted && networkCostType != NetworkCostType.Unknown);
        }

        public DavEnvironment GetEnvironment()
        {
            return FileManager.Environment;
        }
    }
}
