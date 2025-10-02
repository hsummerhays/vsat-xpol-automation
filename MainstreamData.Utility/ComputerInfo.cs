// <copyright file="ComputerInfo.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Utility
{
    using System;
    using System.Globalization;
    using System.Management;
    using System.Text;
    using Microsoft.Win32;

    /// <summary>
    /// Provides easy access to computer hardware and software statistics
    /// </summary>
    public static class ComputerInfo
    {
        // TODO: Create a simple interface for getting results from WMI queries into a string or list.

        /// <summary>
        /// Used to convert bytes to megabytes.
        /// </summary>
        private const int Megabyte = 1024 * 1024;

        /// <summary>
        /// Used to convert kilobytes to megabytes.
        /// </summary>
        private const int Kilobyte = 1024;

        /// <summary>
        /// Gets the machine name that the application is running on.
        /// </summary>
        public static string MachineName
        {
            get
            {
                return Environment.MachineName;
            }
        }

        /// <summary>
        /// Gets the Windows System ID
        /// </summary>
        public static string Sid
        {
            get
            {
                // Note: Error handling is done by calling method.

                // Create searcher to look through local and domain accounts.
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "root\\CIMV2", "SELECT SID FROM Win32_Account where localaccount = 1 and sidtype = 1"))
                {
                    // Go through accounts to find right SID.
                    foreach (ManagementObject account in searcher.Get())
                    {
                        using (account)
                        {
                            string sid = account["SID"].ToString();

                            // Make sure SID is of long variety - running as service under system account may return some short SID's.
                            if (sid.Split('-').Length.Equals(8))
                            {
                                // Also include MAC address to make completely unique - hopefully
                                ManagementObjectSearcher nicSearcher = new ManagementObjectSearcher(
                                    "root\\cimv2",
                                    "SELECT * FROM Win32_NetworkAdapter where adaptertype like 'ethernet%' and macaddress like '00%'");
                                string mac = string.Empty;
                                foreach (ManagementObject nic in nicSearcher.Get())
                                {
                                    using (nic)
                                    {
                                        if (nic["MACAddress"] != null)
                                        {
                                            mac = nic["MACAddress"].ToString();
                                            break;  // Only need first MAC
                                        }
                                    }
                                }

                                return sid.Substring(0, sid.LastIndexOf("-", StringComparison.Ordinal)) + " " + mac;
                            }
                        }
                    }
                }

                throw new ManagementException("Unable to Obtain SID.");
            }
        }

        /// <summary>
        /// Gets the PC's physical memory in megabytes.
        /// </summary>
        public static int PhysicalMemoryMB
        {
            get
            {
                // Win32_ComputerSystem returns TotalPhysicalMemory in bytes.
                long ram = Convert.ToInt64(QueryWmi("Select TotalPhysicalMemory from Win32_ComputerSystem", "TotalPhysicalMemory"), CultureInfo.InvariantCulture);
                return (int)(ram / (long)ComputerInfo.Megabyte) + 1;
            }
        }

        /// <summary>
        /// Gets the PC's used memory in megabytes including virtual memory.
        /// In Windows XP it doesn't directly match taskmgr - perhaps a limitation of WMI.
        /// </summary>
        public static int TotalMemoryUsageMB
        {
            get
            {
                // Everything here except the return value is in kilobyes.
                const string Query = "Select * from Win32_OperatingSystem";
                int totalVirtual = Convert.ToInt32(QueryWmi(Query, "TotalVirtualMemorySize"), CultureInfo.InvariantCulture);
                int freeVirtual = Convert.ToInt32(QueryWmi(Query, "FreeVirtualMemory"), CultureInfo.InvariantCulture);
                int totalPhysical = Convert.ToInt32(QueryWmi(Query, "TotalVisibleMemorySize"), CultureInfo.InvariantCulture);
                int freePhysical = Convert.ToInt32(QueryWmi(Query, "FreePhysicalMemory"), CultureInfo.InvariantCulture);

                // Windows XP version returns 5.  Windows 7 returns 6.
                int windowsVersion = Convert.ToInt32(ComputerInfo.WindowsVersionNumber.Split('.')[0], CultureInfo.InvariantCulture);
                int usedVirtual = totalVirtual - freeVirtual;
                int usedPhysical = totalPhysical - freePhysical;
                int usedTotal = usedVirtual + (windowsVersion < 6 ? usedPhysical : 0);

                return usedTotal / ComputerInfo.Kilobyte;
            }
        }

        /// <summary>
        /// Gets the utilization on all processors combined into a single percentage value.
        /// </summary>
        public static int ProcessorUtilization
        {
            get
            {
                // Get CPU utilization - get average on hyperthreaded and dual core
                int cpuUtilization = 0;
                int total = 0;
                int count = 0;

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject processor in searcher.Get())
                    {
                        using (processor)
                        {
                            total += Convert.ToInt16(processor["LoadPercentage"], CultureInfo.InvariantCulture);
                            count += 1;
                        }
                    }
                }

                cpuUtilization = total / count;
                return cpuUtilization;
            }
        }

        /// <summary>
        /// Gets the Windows version using WMI (e.g. Microsoft Windows XP Professional 5.1.2600 (Service Pack 3))
        /// </summary>
        public static string WindowsVersionString
        {
            get
            {
                // Note: Error handling is done by calling method.

                // Create searcher to retrieve OS info.
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "root\\CIMV2", "SELECT Caption, CSDVersion, Version FROM Win32_OperatingSystem"))
                {
                    // Get results
                    foreach (ManagementObject info in searcher.Get())
                    {
                        using (info)
                        {
                            string caption = info["Caption"].ToString();          // e.g. Microsoft Windows XP Professional
                            string csdVersion = info["CSDVersion"] == null ? string.Empty : 
                                string.Format(CultureInfo.InvariantCulture, "({0})", info["CSDVersion"].ToString());    // e.g. Service Pack 3
                            string version = info["Version"].ToString();          // e.g. 5.1.2600
                            return string.Format(
                                CultureInfo.InvariantCulture,
                                "{0} {1} {2}",
                                caption,
                                version,
                                csdVersion);
                        }
                    }
                }

                throw new ManagementException("Unable to Obtain OS Info.");
            }
        }

        /// <summary>
        /// Gets the Windows version number (e.g. for Windows 7 it could be 6.1.7600)
        /// </summary>
        public static string WindowsVersionNumber
        {
            get
            {
                return QueryWmi("SELECT Version FROM Win32_OperatingSystem", "Version").Replace("\r\n", string.Empty);
            }
        }

        /// <summary>
        /// Gets information about running processes.
        /// </summary>
        /// <returns>Name and memory usage of running processes in a space delmited string.</returns>
        public static string RunningProcesses
        {
            get
            {
                StringBuilder processList = new StringBuilder();

                // Get list of running processes from WMI.
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Name, WorkingSetSize FROM Win32_Process"))
                {
                    // Go through each process to find matching process.
                    foreach (ManagementObject process in searcher.Get())
                    {
                        using (process)
                        {
                            string memoryMegabytes = (Convert.ToInt64(process["WorkingSetSize"], CultureInfo.InvariantCulture) / 
                                (long)ComputerInfo.Megabyte).ToString(CultureInfo.InvariantCulture);
                            processList.AppendLine(process["Name"] + " " + memoryMegabytes + "MB");
                        }
                    }
                }

                return processList.ToString();
            }
        }

        /// <summary>
        /// Get data from registry under Local_Machine at the specified location and allow to include data in subkeys.
        /// </summary>
        /// <param name="keyPath">Path under Local_Machine of the key to get (e.g. "software\microsoft\internet explorer").</param>
        /// <returns>List of keys and values separated by carriage return.</returns>
        public static string GetRegistryLocalMachine(string keyPath)
        {
            // TODO: Raise an error when the specified key is not found.
            // TODO: Sort name/value pairs by name.
            string fullPath = "My Computer\\HKEY_LOCAL_MACHINE\\" + keyPath;
            StringBuilder namesAndValues = new StringBuilder();
            RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath);
            if (key == null)
            {
                namesAndValues.AppendLine("Key not found: " + fullPath);
            }
            else
            {
                namesAndValues.AppendLine(fullPath);
                foreach (string valueName in key.GetValueNames())
                {
                    RegistryValueKind rvk = key.GetValueKind(valueName);
                    StringBuilder value = new StringBuilder();
                    switch (rvk)
                    {
                        case RegistryValueKind.MultiString:
                            string[] values = (string[])key.GetValue(valueName);

                            for (int i = 0; i < values.Length; i++)
                            {
                                if (i != 0)
                                {
                                    value.Append(",");
                                }

                                value.Append(values[i]);
                            }

                            break;

                        case RegistryValueKind.Binary:
                            byte[] bytes = (byte[])key.GetValue(valueName);
                            for (int i = 0; i < bytes.Length; i++)
                            {
                                // Display each byte as two hexadecimal digits.
                                value.Append(string.Format(CultureInfo.InvariantCulture, " {0:X2}", bytes[i]));
                            }

                            Console.WriteLine();
                            break;

                        default:
                            value.Append(key.GetValue(valueName).ToString());
                            break;
                    }

                    namesAndValues.AppendLine(valueName + '=' + value.ToString());
                }
            }

            return namesAndValues.ToString();
        }

        /// <summary>
        /// Gets the name of the Windows user that is running a given process.
        /// </summary>
        /// <param name="processName">Name of the process to check.</param>
        /// <returns>Name of the user that the process is running under.</returns>
        public static string GetProcessUser(string processName)
        {
            string processUser = string.Empty;

            // Get list of running processes from WMI - must select * for GetOwner to work.
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Process WHERE Name = '" + processName + "'"))
            {
                // Go through each process to find matching process.
                foreach (ManagementObject process in searcher.Get())
                {
                    using (process)
                    {
                        // Retrieve the name of the process owner.
                        string[] temp = new string[2];
                        process.InvokeMethod("GetOwner", (object[])temp);
                        processUser = temp[1] + "\\" + temp[0];

                        // TODO: Decide what to do when there is more than one instance of the process running.
                        break;
                    }
                }
            }

            return processUser;
        }

        /// <summary>
        /// Retrieves percent of drive space that is in use.
        /// </summary>
        /// <param name="driveLetter">Letter representation of drive to get the value from.</param>
        /// <returns>Percent of drive space that is in use.</returns>
        public static int? GetDriveUtilization(string driveLetter)
        {
            //// Note: Error handling is done by calling method

            double capacity = 0;
            double? used = null;   // Return null if drive doesn't exist.

            // Retrieve list of logical disks from WMI
            using (ManagementObjectSearcher disks = new ManagementObjectSearcher("select size, freespace from win32_logicaldisk where deviceid=\"" + driveLetter + ":\""))
            {
                // Pull out utilization information.
                foreach (ManagementObject disk in disks.Get())
                {
                    using (disk)
                    {
                        capacity = Convert.ToDouble(disk["Size"], CultureInfo.InvariantCulture);
                        used = capacity - Convert.ToDouble(disk["FreeSpace"], CultureInfo.InvariantCulture);
                    }

                    break;  // Only need first result
                }
            }

            // Return the capacity.
            if (used == null || capacity.Equals(0))
            {
                return null;  // Treat drive as non-existent if has no capacity (e.g. DVD drives return zero).
            }
            else
            {
                return Convert.ToInt32((used / capacity) * 100, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Queries WMI and returns all result rows as lines in a text string.
        /// </summary>
        /// <param name="managementScope">E.g. "root\\CIMV2".</param>
        /// <param name="query">The WMI select statement (e.g. Select * from Win32_ComputerSystem).</param>
        /// <param name="fieldName">The field name to retrieve values from.</param>
        /// <returns>A string containing a list of field values - one line for each row.</returns>
        public static string QueryWmi(string managementScope, string query, string fieldName)
        {
            // TODO: Enhance if desired - see WmiQueryBrowser app I (Hugh) wrote - may be some helpful code or comments.
            StringBuilder text = new StringBuilder();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(managementScope, query))
            {
                // Go through each process to find matching process.
                foreach (ManagementObject row in searcher.Get())
                {
                    using (row)
                    {
                        text.AppendLine(row[fieldName].ToString());
                    }
                }
            }

            return text.ToString();
        }

        /// <summary>
        /// Queries root\cimv2 based on select statement you provide.
        /// </summary>
        /// <param name="query">The WMI select statement (e.g. Select * from Win32_ComputerSystem).</param>
        /// <param name="fieldName">The field name to retrieve values from.</param>
        /// <returns>A string containing a list of field values - one line for each row.</returns>
        public static string QueryWmi(string query, string fieldName)
        {
            return ComputerInfo.QueryWmi("root\\cimv2", query, fieldName);
        }
    }
}