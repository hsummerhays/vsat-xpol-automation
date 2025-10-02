// -----------------------------------------------------------------------
// <copyright file="VsatXpolLmpList.cs" company="Mainstream Data, Inc.">
// Mainstream Data, Inc.
// </copyright>
// -----------------------------------------------------------------------

namespace MainstreamData.Monitoring.VsatXpol
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Web;
    using System.Web.SessionState;

    /// <summary>
    /// Holds a list of <see cref="VsatXpolLmp"/> objects for reference throughout a given http session.
    /// </summary>
    public static class VsatXpolLmpList
    {
        /// <summary>
        /// List of VsatXpolLmp's within the current application (all sessions for ASP.Net).
        /// </summary>
        private static Dictionary<string, VsatXpolLmp> dictionary = new Dictionary<string, VsatXpolLmp>();

        /// <summary>
        /// Gets list of VsatXpolLmp's within the current application (all sessions for ASP.Net).
        /// </summary>
        public static Dictionary<string, VsatXpolLmp> Dictionary 
        {
            get
            {
                return dictionary;
            }
        }

        /// <summary>
        /// Adds a new instance of the <see cref="VsatXpolLmp"/> class to the list.
        /// </summary>
        /// <param name="satelliteName">The name of the satellite.</param>
        /// <param name="networks">A list of the networks available on the satellite.</param>
        /// <param name="wcfAddress">The Windows Communication Foundation address for connecting to the VsatXpolRmp (e.g. http://192.168.170.230:8000/ServiceModel/vsatxpolrmp ).</param>
        /// <exception cref="InvalidOperationException">Is thrown if unable to open connection to RMP.</exception>
        /// <returns>The newly added <see cref="VsatXpolLmp"/>.</returns>
        public static VsatXpolLmp Add(string satelliteName, string networks, string wcfAddress)
        {
            VsatXpolLmp lmp = new VsatXpolLmp(satelliteName, networks, wcfAddress);
            VsatXpolLmpList.Dictionary.Add(satelliteName, lmp);
            return lmp;
        }

        /// <summary>
        /// Determines whether <see cref="VsatXpolLmpList"/> contains an entry for the specified satelliteName.
        /// </summary>
        /// <param name="satelliteName">The name of the satellite.</param>
        /// <returns>True if satelliteName is found in list.</returns>
        public static bool Contains(string satelliteName)
        {
            return VsatXpolLmpList.Dictionary.ContainsKey(satelliteName);
        }

        /// <summary>
        /// Gets the <see cref="VsatXpolLmp"/> for the specified satelliteName.
        /// </summary>
        /// <param name="satelliteName">The name of the satellite.</param>
        /// <returns>The LMP for the specified satelliteName.</returns>
        public static VsatXpolLmp GetLmp(string satelliteName)
        {
            return VsatXpolLmpList.Dictionary[satelliteName];
        }
    }
}
