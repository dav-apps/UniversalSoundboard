using davClassLibrary.Common;
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
    }
}
