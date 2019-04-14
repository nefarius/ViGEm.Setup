using Microsoft.Deployment.WindowsInstaller;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ViGEm.Setup.CustomAction.Core;
using ViGEm.Setup.CustomAction.Util;

namespace ViGEm.Setup.CustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult RemoveAllViGEmBusInstances(Session session)
        {
            session.Log("Begin RemoveAllViGEmBusInstances");

            var result = ActionResult.Success;
            var isSilent = session.CustomActionData["UILevel"] == "2";

            // Loop through all instances (if any)
            while (Devcon.FindDeviceByInterfaceId(ViGEmBusDevice.InterfaceGuid, out var path, out var instanceId))
            {
                // Grab device details via WMI
                var details = ViGEmBusDevice.GetDeviceDetails(instanceId, path);

                session.Log($"Found ViGEmBus device: {instanceId} ({path}), " +
                            $"Manufacturer: {details.Manufacturer}, Version: {details.DriverVersion}");

                try
                {
                    // Attempt device removal
                    Devcon.RemoveDeviceInstance(ViGEmBusDevice.InterfaceGuid, instanceId,
                        out var rebootRequired);

                    session.Log($"Successfully removed {instanceId}, reboot required: {rebootRequired}");

                    // Removed completely, advance to next instance
                    if (!rebootRequired) continue;

                    // Display info message if interactive
                    if (!isSilent)
                        session.Message(InstallMessage.Warning | (InstallMessage)MessageBoxButtons.OK, new Record
                        {
                            FormatString = $"To complete the removal of {instanceId} the system requires a reboot. " +
                                           "The setup will end now. Please restart your machine and run setup again."
                        });

                    result = ActionResult.Failure;
                    break;
                }
                catch (Win32Exception ex)
                {
                    session.Log($"Unexpected Win32Exception on Devcon.RemoveDeviceInstance: {ex}");

                    // Display error message if interactive
                    if (!isSilent)
                        session.Message(InstallMessage.Error | (InstallMessage)MessageBoxButtons.OK, new Record
                        {
                            FormatString = ex.Message
                        });

                    result = ActionResult.Failure;
                    break;
                }
            }

            session.Log("End RemoveAllViGEmBusInstances");

            return result;
        }

        [CustomAction]
        public static ActionResult InstallViGEmBusDevice(Session session)
        {
            session.Log("Begin InstallViGEmBusDevice");

            var result = ActionResult.Success;
            var appDir = session.CustomActionData["APPDIR"];
            var fullInfPath = Path.Combine(
                appDir,
                $@"drivers\Win{(Environment.OSVersion.Version.Major == 10 ? "10" : "6")}",
                "ViGEmBus.inf");

            session.Log($"Attempting to install driver {fullInfPath}");

            try
            {
                Devcon.CreateDeviceNode(
                    ViGEmBusDevice.DeviceClassName,
                    ViGEmBusDevice.DeviceClassGuid,
                    ViGEmBusDevice.HardwareId
                );

                var ret = Devcon.UpdateDeviceDriver(
                    ViGEmBusDevice.HardwareId,
                    fullInfPath,
                    out var rebootRequired
                );

                if (!ret)
                {
                    session.Log(
                        $"Devcon.UpdateDeviceDriver failed: {new Win32Exception(Marshal.GetLastWin32Error())}");
                    result = ActionResult.Failure;
                }
            }
            catch (Win32Exception ex)
            {
                session.Log($"Devcon.CreateDeviceNode failed: {ex}");
                result = ActionResult.Failure;
            }

            session.Log("End InstallViGEmBusDevice");

            return result;
        }
    }
}