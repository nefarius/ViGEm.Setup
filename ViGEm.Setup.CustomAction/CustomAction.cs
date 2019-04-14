using Microsoft.Deployment.WindowsInstaller;
using System.ComponentModel;
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
            var result = ActionResult.Failure;
            var isSilent = session.CustomActionData["UILevel"] == "2";
            var appDir = session.CustomActionData["APPDIR"];

            session.Log("Begin RemoveAllViGEmBusInstances");

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
                    var ret = Devcon.RemoveDeviceInstance(ViGEmBusDevice.InterfaceGuid, instanceId,
                        out var rebootRequired);

                    if (ret)
                        session.Log($"Successfully removed {instanceId}, reboot required: {rebootRequired}");

                    // TODO: handle removal error


                    if (rebootRequired)
                    {
                        if (!isSilent)
                            session.Message(InstallMessage.Warning | (InstallMessage)MessageBoxButtons.OK, new Record
                            {
                                FormatString = $"To complete the removal of {instanceId} the system requires a reboot. " +
                                               "The setup will end now. Please restart your machine and run setup again."
                            });

                        result = ActionResult.Failure;
                        break;
                    }

                    //session.Message(InstallMessage.User | (InstallMessage) MessageBoxButtons.OK, new Record
                    //{
                    //    FormatString = $"{instanceId} - {ret} - {new Win32Exception(Marshal.GetLastWin32Error())}"
                    //});
                }
                catch (Win32Exception ex)
                {
                    session.Message(InstallMessage.Error | (InstallMessage)MessageBoxButtons.OK, new Record
                    {
                        FormatString = ex.Message
                    });
                }
            }

            session.Log("End RemoveAllViGEmBusInstances");

            return result;
        }
    }
}