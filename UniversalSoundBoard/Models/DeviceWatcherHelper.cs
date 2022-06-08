using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Devices.Enumeration;

namespace UniversalSoundboard.Models
{
    public class DeviceWatcherHelper
    {
        DeviceWatcher deviceWatcher;
        ObservableCollection<DeviceInfo> devices;
        public List<DeviceInfo> Devices
        {
            get => devices.ToList();
        }

        public event EventHandler<EventArgs> DevicesChanged;

        public DeviceWatcherHelper(DeviceClass deviceClass)
        {
            deviceWatcher = DeviceInformation.CreateWatcher(deviceClass);
            devices = new ObservableCollection<DeviceInfo>();

            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;

            deviceWatcher.Start();
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            devices.Add(new DeviceInfo(deviceInfo));
            DevicesChanged?.Invoke(this, new EventArgs());
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfo)
        {
            foreach (DeviceInfo device in devices)
            {
                if (device.Id == deviceInfo.Id) device.Update(deviceInfo);
            }

            DevicesChanged?.Invoke(this, new EventArgs());
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfo)
        {
            foreach (DeviceInfo device in devices)
            {
                if (device.Id == deviceInfo.Id)
                {
                    devices.Remove(device);
                    break;
                }
            }

            DevicesChanged?.Invoke(this, new EventArgs());
        }
    }
}
