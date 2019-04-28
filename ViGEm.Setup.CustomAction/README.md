# WiX Toolset Custom Actions for driver management

A collection of [Custom Actions](https://docs.microsoft.com/en-us/windows/desktop/msi/custom-actions) for interacting with device drivers from an MSI-based installer.

## About

The Windows Installer (MSI) subsystem lacks a robust set of integrated utilities to deal with device driver installation, removal and updating. This project bridges the gap between the Windows Installer ecosystem and the rather hostile yet powerful low-level [SetupAPI](https://docs.microsoft.com/en-us/windows-hardware/drivers/install/setupapi) wrapped in a managed library.

Currently the following Custom Actions have been implemented:

### `RemoveAllViGEmBusInstances`

Enumerates all existing ViGEmBus devices (if any) identified by interface GUID and attempts to remove them. If an instance is currently in use, an error is displayed if in interactive mode and requests the user to reboot the system, which will lead to the removal of the instance. If invoked in silent mode, the setup ends with a return code indicating a reboot is required.

### `InstallViGEmBusDevice`

Creates a virtual device node ("fake" Hardware ID) for the driver to attach to. Then invokes driver installtion onto created device node. The method distinguishes between Kernel Major Version 6 (Windows 7 to 8.1) and 10 (Windows 10) and automatically chooses the compatible `INF` file. This is required because the WHQL process will spit out different files for different OS versions which has to be taken into account during installation.

## Sources

- [How to create fully fledged C# custom actions](https://www.advancedinstaller.com/user-guide/qa-c-sharp-ca.html)
- [WiX Toolset v3.x](https://github.com/wixtoolset/wix3)
- [ManagedDevcon](https://github.com/nefarius/ManagedDevcon)
- [Creating Custom Action for WIX Written in Managed Code without Votive](https://www.codeproject.com/Articles/132918/Creating-Custom-Action-for-WIX-Written-in-Managed)
- [Windows Installer Properties](https://www.advancedinstaller.com/user-guide/properties.html)
- [Install a component or run a custom action based on a specific Windows version](https://www.advancedinstaller.com/user-guide/qa-OS-dependent-install.html#windows-version)
- [Why is 0xe0000235 returned when calling SetupDiCallClassInstaller when using WOW64](https://social.msdn.microsoft.com/Forums/windowsdesktop/en-US/99d77dcc-c076-421e-92fc-c0261d0aa109/why-is-0xe0000235-returned-when-calling-setupdicallclassinstaller-when-using-wow64?forum=windowsgeneraldevelopmentissues)
- [Is there alternative way to access session details in deferred custom action?](https://stackoverflow.com/q/7306367)
