using Microsoft.Deployment.WindowsInstaller;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ViGEm.Setup.CustomAction.Core;
using ViGEm.Setup.CustomAction.Util;

namespace ViGEm.Setup.CustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CustomAction1(Session session)
        {
            session.Log("Begin CustomAction1");

            session.Message(InstallMessage.User | (InstallMessage)MessageBoxButtons.OK, new Record
            {
                FormatString = $"{Environment.Is64BitProcess}"
            });

            var index = 0;

            while (Devcon.FindDeviceByInterfaceId(ViGEmBusDevice.InterfaceGuid, out var path, out var instanceId, index))
            {
                try
                {
                    var ret = Devcon.RemoveDeviceInstance(ViGEmBusDevice.InterfaceGuid, instanceId, out var rebootRequired);

                    session.Message(InstallMessage.User | (InstallMessage) MessageBoxButtons.OK, new Record
                    {
                        FormatString = $"{instanceId} - {ret} - {new Win32Exception(Marshal.GetLastWin32Error())}"
                    });
                }
                catch (Win32Exception ex)
                {
                    session.Message(InstallMessage.Error | (InstallMessage)MessageBoxButtons.OK, new Record
                    {
                        FormatString = ex.Message
                    });
                }

                index++;
            }

            return ActionResult.Failure;
        }
    }
}
