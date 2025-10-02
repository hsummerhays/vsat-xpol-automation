// <copyright file="ApplicationInfo.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Utility
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    /// <summary>
    /// Provides access to information about the running application.
    /// Portions of this code adapted from post by Paul Sinnema.
    /// Code to get parent process posted by Michael Hale.
    /// </summary>
    public static class ApplicationInfo
    {
        /// <summary>
        /// Holds assembly name object for easy access.
        /// </summary>
        private static AssemblyName assemblyName =
            Assembly.GetEntryAssembly().GetName();

        /// <summary>
        /// Holds entry assembly object for easy access.
        /// </summary>
        private static Assembly assembly = Assembly.GetEntryAssembly();

        /// <summary>
        /// Gets the assebly name of the running application from project properties - application.
        /// </summary>
        public static string Name
        {
            get
            {
                return assemblyName.Name;
            }
        }

        /// <summary>
        /// Gets the drive path to the application.
        /// </summary>
        public static string Path
        {
            get
            {
                return System.IO.Path.GetDirectoryName(
                    ApplicationInfo.assembly.CodeBase);
            }
        }

        /// <summary>
        /// Gets the assembly version of the application.
        /// </summary>
        public static string Version
        {
            get
            {
                return assemblyName.Version.ToString();
            }
        }

        /// <summary>
        /// Gets the title field value from project properties - application - assembly info.
        /// </summary>
        public static string Title
        {
            get
            {
                return CustomAttributes<AssemblyTitleAttribute>().Title;
            }
        }

        /// <summary>
        /// Gets the description field value from project properties - application - assembly info.
        /// </summary>
        public static string Description
        {
            get
            {
                return CustomAttributes<AssemblyDescriptionAttribute>().Description;
            }
        }

        /// <summary>
        /// Gets the company field value from project properties - application - assembly info.
        /// </summary>
        public static string Company
        {
            get
            {
                return CustomAttributes<AssemblyCompanyAttribute>().Company;
            }
        }

        /// <summary>
        /// Gets the product field value from project properties - application - assembly info.
        /// </summary>
        public static string Product
        {
            get
            {
                return CustomAttributes<AssemblyProductAttribute>().Product;
            }
        }

        /// <summary>
        /// Gets the copyright field value from project properties - application - assembly info.
        /// </summary>
        public static string Copyright
        {
            get
            {
                return CustomAttributes<AssemblyCopyrightAttribute>().Copyright;
            }
        }

        /// <summary>
        /// Gets the trademark field value from project properties - application - assembly info.
        /// </summary>
        public static string Trademark
        {
            get
            {
                return CustomAttributes<AssemblyTrademarkAttribute>().Trademark;
            }
        }

        /// <summary>
        /// Gets the fileversion field value from project properties - application - assembly info.
        /// </summary>
        public static string FileVersion
        {
            get
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fvi.FileVersion;
            }
        }

        /// <summary>
        /// Gets the GUID field value from project properties - application - assembly info.
        /// </summary>
        public static string Guid
        {
            get
            {
                return CustomAttributes<System.Runtime.InteropServices.GuidAttribute>().Value;
            }
        }

        /// <summary>
        /// Gets the file name of the application.
        /// </summary>
        public static string FileName
        {
            get
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fvi.OriginalFilename;
            }
        }

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        public static Process ParentProcess
        {
            get
            {
                return FindPidFromIndexedProcessName(FindIndexedProcessName(Process.GetCurrentProcess().Id));
            }
        }

        /// <summary>
        /// Retrieves custom attributes of the assembly using reflection.
        /// </summary>
        /// <typeparam name="T">The attribute to retrieve.</typeparam>
        /// <returns>The specified attribute.</returns>
        private static T CustomAttributes<T>()
            where T : Attribute
        {
            object[] customAttributes = assembly.GetCustomAttributes(typeof(T), false);

            if ((customAttributes != null) && (customAttributes.Length > 0))
            {
                return (T)customAttributes[0];
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Finds the process index name for a given process id.
        /// </summary>
        /// <param name="pid">The process id to search for.</param>
        /// <returns>The process index name.</returns>
        private static string FindIndexedProcessName(int pid)
        {
            string processName = Process.GetProcessById(pid).ProcessName;
            Process[] processesByName = Process.GetProcessesByName(processName);
            string processIndexedName = null;
            for (int index = 0; index < processesByName.Length; index++)
            {
                processIndexedName = index == 0 ? processName : processName + "#" + index;
                PerformanceCounter processId = new PerformanceCounter("Process", "ID Process", processIndexedName);
                if ((int)processId.NextValue() == pid)
                {
                    return processIndexedName;
                }
            }

            return processIndexedName;
        }
        
        /// <summary>
        /// Finds a process using indexed process name.
        /// </summary>
        /// <param name="indexedProcessName">The indexed process name.</param>
        /// <returns>The process matching the indexed process name.</returns>
        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            PerformanceCounter parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            return Process.GetProcessById((int)parentId.NextValue());
        }
    }
}