using System;
using ViGEm.Setup.CustomAction.Util;

namespace DebuggingConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var busGuid = Guid.Parse("{96E42B22-F5E9-42F8-B043-ED0F932F014F}");

            var index = 0;

            while (Devcon.FindDeviceByInterfaceId(busGuid, out var path, out var instanceId, index))
            {
                var ret = Devcon.RemoveDeviceInstance(busGuid, instanceId);

                index++;
            }
        }
    }
}
