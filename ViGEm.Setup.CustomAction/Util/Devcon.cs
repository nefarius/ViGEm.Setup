using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ViGEm.Setup.CustomAction.Util
{
    /// <summary>
    ///     Managed wrapper for common SetupAPI operations.
    /// </summary>
    public static partial class Devcon
    {
        /// <summary>
        ///     Invokes the installation of a driver via provided .INF file.
        /// </summary>
        /// <param name="fullInfPath">An absolute path to the .INF file to install.</param>
        /// <param name="rebootRequired">True if a machine reboot is required, false otherwise.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool InstallDriver(string fullInfPath, out bool rebootRequired)
        {
            return DiInstallDriver(IntPtr.Zero, fullInfPath, DIIRFLAG_FORCE_INF, out rebootRequired);
        }

        /// <summary>
        ///     Creates a virtual device node (hardware ID) in the provided device class.
        /// </summary>
        /// <param name="className">The device class name.</param>
        /// <param name="classGuid">The GUID of the device class.</param>
        /// <param name="node">The node path terminated by two null characters.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool CreateDeviceNode(string className, Guid classGuid, string node)
        {
            var deviceInfoSet = (IntPtr) (-1);
            var deviceInfoData = new SP_DEVINFO_DATA();

            try
            {
                deviceInfoSet = SetupDiCreateDeviceInfoList(ref classGuid, IntPtr.Zero);

                if (deviceInfoSet == (IntPtr) (-1))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                if (!SetupDiCreateDeviceInfo(
                    deviceInfoSet,
                    className,
                    ref classGuid,
                    null,
                    IntPtr.Zero,
                    DICD_GENERATE_ID,
                    ref deviceInfoData
                ))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!SetupDiSetDeviceRegistryProperty(
                    deviceInfoSet,
                    ref deviceInfoData,
                    SPDRP_HARDWAREID,
                    node,
                    node.Length * 2
                ))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                if (!SetupDiCallClassInstaller(
                    DIF_REGISTERDEVICE,
                    deviceInfoSet,
                    ref deviceInfoData
                ))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (deviceInfoSet != (IntPtr) (-1))
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return true;
        }

        /// <summary>
        ///     Instructs the system to re-enumerate hardware devices.
        /// </summary>
        /// <returns>True on success, false otherwise.</returns>
        public static bool RefreshDevices()
        {
            if (CM_Locate_DevNode_Ex(
                    out var devRoot,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero
                ) != CR_SUCCESS)
                return false;

            return CM_Reenumerate_DevNode_Ex(devRoot, 0, IntPtr.Zero) == CR_SUCCESS;
        }

        /// <summary>
        ///     Given an INF file and a hardware ID, this function installs updated drivers for devices that match the hardware ID.
        /// </summary>
        /// <param name="hardwareId">A string that supplies the hardware identifier to match existing devices on the computer.</param>
        /// <param name="fullInfPath">A string that supplies the full path file name of an INF file.</param>
        /// <param name="rebootRequired">A variable that indicates whether a restart is required and who should prompt for it.</param>
        /// <returns>
        ///     The function returns TRUE if a device was upgraded to the specified driver.
        ///     Otherwise, it returns FALSE and the logged error can be retrieved with a call to GetLastError.
        /// </returns>
        public static bool UpdateDeviceDriver(string hardwareId, string fullInfPath, out bool rebootRequired)
        {
            return UpdateDriverForPlugAndPlayDevices(
                IntPtr.Zero,
                hardwareId,
                fullInfPath,
                INSTALLFLAG_FORCE | INSTALLFLAG_NONINTERACTIVE,
                out rebootRequired
            );
        }

        /// <summary>
        ///     Removed a device node identified by class GUID, path and instance ID.
        /// </summary>
        /// <param name="classGuid">The device class GUID.</param>
        /// <param name="instanceId">The instance ID.</param>
        /// <param name="rebootRequired">True if a reboot is required to complete the uninstall action, false otherwise.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool RemoveDeviceInstance(Guid classGuid, string instanceId, out bool rebootRequired)
        {
            var deviceInfoSet = IntPtr.Zero;
            var installParams = Marshal.AllocHGlobal(584); // Max struct size on x64 platform

            try
            {
                var deviceInfoData = new SP_DEVINFO_DATA();
                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                deviceInfoSet = SetupDiGetClassDevs(
                    ref classGuid,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_DEVICEINTERFACE
                );

                if (SetupDiOpenDeviceInfo(
                    deviceInfoSet,
                    instanceId,
                    IntPtr.Zero,
                    0,
                    ref deviceInfoData
                ))
                {
                    var props = new SP_REMOVEDEVICE_PARAMS {ClassInstallHeader = new SP_CLASSINSTALL_HEADER()};

                    props.ClassInstallHeader.cbSize = Marshal.SizeOf(props.ClassInstallHeader);
                    props.ClassInstallHeader.InstallFunction = DIF_REMOVE;

                    props.Scope = DI_REMOVEDEVICE_GLOBAL;
                    props.HwProfile = 0x00;

                    // Prepare class (un-)installer
                    if (SetupDiSetClassInstallParams(
                        deviceInfoSet,
                        ref deviceInfoData,
                        ref props,
                        Marshal.SizeOf(props)
                    ))
                    {
                        // Invoke class installer with uninstall action
                        if (!SetupDiCallClassInstaller(DIF_REMOVE, deviceInfoSet, ref deviceInfoData))
                            throw new Win32Exception(Marshal.GetLastWin32Error());

                        // Fill cbSize field
                        Marshal.WriteInt32(
                            installParams,
                            0, // cbSize is first field, always 32 bits long
                            IntPtr.Size == 4 ? 556 /* x86 size */ : 584 /* x64 size */
                        );

                        // Fill SP_DEVINSTALL_PARAMS struct
                        if (!SetupDiGetDeviceInstallParams(deviceInfoSet, ref deviceInfoData, installParams))
                            throw new Win32Exception(Marshal.GetLastWin32Error());

                        // Grab Flags field of SP_DEVINSTALL_PARAMS (offset of 32 bits)
                        var flags = Marshal.ReadInt32(installParams, Marshal.SizeOf(typeof(uint)));

                        // Test for restart/reboot flags being present
                        rebootRequired = (flags & DI_NEEDRESTART) != 0 || (flags & DI_NEEDREBOOT) != 0;

                        return true;
                    }
                    else throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                else throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                Marshal.FreeHGlobal(installParams);
            }
        }

        /// <summary>
        ///     Searches for devices matching the provided class GUID and returns the device path and instance ID.
        /// </summary>
        /// <param name="target">The class GUID to enumerate.</param>
        /// <param name="path">The device path of the enumerated device.</param>
        /// <param name="instanceId">The instance ID of the enumerated device.</param>
        /// <param name="instance">Optional instance ID (zero-based) specifying the device to process on multiple matches.</param>
        /// <returns>True if at least one device was found with the provided class, false otherwise.</returns>
        public static bool FindDeviceByInterfaceId(Guid target, out string path, out string instanceId,
            int instance = 0)
        {
            var deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA
            {
                cbSize = Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA))
            };

            var da = new SP_DEVINFO_DATA
            {
                cbSize = Marshal.SizeOf(typeof(SP_DEVINFO_DATA))
            };

            var deviceInfoSet = IntPtr.Zero;
            var detailDataBuffer = IntPtr.Zero;
            int bufferSize = 0, memberIndex = 0;

            try
            {
                deviceInfoSet = SetupDiGetClassDevs(
                    ref target,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    DIGCF_PRESENT | DIGCF_DEVICEINTERFACE
                );

                while (SetupDiEnumDeviceInterfaces(
                    deviceInfoSet,
                    IntPtr.Zero,
                    ref target,
                    memberIndex,
                    ref deviceInterfaceData))
                {
                    SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet,
                        ref deviceInterfaceData,
                        IntPtr.Zero,
                        0,
                        ref bufferSize,
                        ref da
                    );

                    detailDataBuffer = Marshal.AllocHGlobal(bufferSize);
                    Marshal.WriteInt32(
                        detailDataBuffer,
                        IntPtr.Size == 4 ? 4 + Marshal.SystemDefaultCharSize : 8
                    );

                    if (SetupDiGetDeviceInterfaceDetail(
                        deviceInfoSet,
                        ref deviceInterfaceData,
                        detailDataBuffer,
                        bufferSize,
                        ref bufferSize,
                        ref da
                    ))
                    {
                        var pDevicePathName = detailDataBuffer + 4;

                        path = (Marshal.PtrToStringAuto(pDevicePathName) ?? string.Empty).ToUpper();

                        if (memberIndex == instance)
                        {
                            const int nBytes = 256;
                            var ptrInstanceBuf = Marshal.AllocHGlobal(nBytes);

                            CM_Get_Device_ID(da.DevInst, ptrInstanceBuf, nBytes, 0);

                            instanceId = (Marshal.PtrToStringAuto(ptrInstanceBuf) ?? string.Empty).ToUpper();

                            Marshal.FreeHGlobal(ptrInstanceBuf);

                            return true;
                        }
                    }

                    memberIndex++;
                }
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                if (detailDataBuffer != IntPtr.Zero)
                    SetupDiDestroyDeviceInfoList(detailDataBuffer);
            }

            path = instanceId = string.Empty;
            return false;
        }
    }
}