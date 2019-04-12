using Microsoft.Deployment.WindowsInstaller;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ViGEm.Setup.CustomAction.Util;

namespace ViGEm.Setup.CustomAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CustomAction1(Session session)
        {
            session.Log("Begin CustomAction1");

            var busGuid = Guid.Parse("{96E42B22-F5E9-42F8-B043-ED0F932F014F}");

            var index = 0;

            while (Devcon.FindDeviceByInterfaceId(busGuid, out var path, out var instanceId, index))
            {
                MessageBox.Show($"{path} - {instanceId}");

                index++;
            }

            return ActionResult.Success;
        }
    }
}
