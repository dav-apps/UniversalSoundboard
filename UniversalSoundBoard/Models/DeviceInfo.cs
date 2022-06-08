using Windows.Devices.Enumeration;

namespace UniversalSoundboard.Models
{
    public class DeviceInfo
    {
        public DeviceInformation DeviceInformation { get; }
        public string Id => DeviceInformation.Id;
        public string Name => DeviceInformation.Name;

        public DeviceInfo(DeviceInformation deviceInfo)
        {
            DeviceInformation = deviceInfo;
        }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);
        }
    }
}
