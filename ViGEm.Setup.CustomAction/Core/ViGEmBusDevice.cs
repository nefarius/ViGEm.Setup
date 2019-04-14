using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using ViGEm.Setup.CustomAction.Util;

namespace ViGEm.Setup.CustomAction.Core
{
    public class ViGEmBusDevice
    {
        public static Guid ClassGuid => Guid.Parse("{96E42B22-F5E9-42F8-B043-ED0F932F014F}");

        public static IEnumerable<ViGEmBusDevice> Devices
        {
            get
            {
                var list = new List<ViGEmBusDevice>();
                var instance = 0;

                while (Devcon.FindDeviceByInterfaceId(ClassGuid, out var path, out var instanceId, instance++))
                    using (var objSearcher =
                        new ManagementObjectSearcher(
                            $"Select * from Win32_PnPSignedDriver Where DeviceID = '{instanceId.Replace("\\", "\\\\")}'")
                    )
                    {
                        using (var objCollection = objSearcher.Get())
                        {
                            var device = objCollection.Cast<ManagementObject>().First();

                            list.Add(new ViGEmBusDevice
                            {
                                DevicePath = path,
                                InstanceId = instanceId,
                                DeviceName = device["DeviceName"].ToString(),
                                DriverVersion = Version.Parse(device["DriverVersion"].ToString()),
                                Manufacturer = device["Manufacturer"].ToString(),
                                DriverProviderName = device["DriverProviderName"].ToString()
                            });
                        }
                    }

                return list;
            }
        }

        public string DevicePath { get; private set; }

        public string InstanceId { get; private set; }

        public string DeviceName { get; private set; }

        public Version DriverVersion { get; private set; }

        public string Manufacturer { get; private set; }

        public string DriverProviderName { get; private set; }

        #region Equality

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ViGEmBusDevice) obj);
        }

        protected bool Equals(ViGEmBusDevice other)
        {
            return string.Equals(DevicePath, other.DevicePath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(DevicePath);
        }

        public static bool operator ==(ViGEmBusDevice left, ViGEmBusDevice right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ViGEmBusDevice left, ViGEmBusDevice right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}