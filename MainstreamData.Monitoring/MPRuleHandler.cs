// <copyright file="MPRuleHandler.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;

    //// TODO: This is an untested class derived from a c++ class - see if it is even needed.

    /// <summary>
    /// Stores rules (aka SiteParameters) and handles the "DownTime" rule.
    /// </summary>
    public class MPRuleHandler
    {
        /// <summary>
        /// Name used for default parameters.
        /// </summary>
        private readonly string defaultName = "Default";

        /// <summary>
        /// Name used for the downtime group.
        /// </summary>
        private readonly string downtimeGroup = "DownTime";

        /// <summary>
        /// A dictionary object within a dictionary object to create a sort of 
        /// three dimensional array whose elements can be accessed by name.
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> rules 
            = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Initializes a new instance of the MPRuleHandler class.
        /// </summary>
        public MPRuleHandler()
        {
            this.SetDefaultDowntime();
        }

        /// <summary>
        /// Allows a group/name/value set to be added to the rules.
        /// </summary>
        /// <param name="group">Name of the rule group</param>
        /// <param name="name">Name of the rule</param>
        /// <param name="value">Value to store</param>
        public void Add(string group, string name, string value)
        {
            // TODO: Make sure there are not problems with case sensitivity.
            // See if the rule needs to be added.
            if (this.rules[group] == null)
            {
                // Add the group/name/value set.
                Dictionary<string, string> nameValuePair = 
                    new Dictionary<string, string>();
                nameValuePair.Add(name, value);
                this.rules.Add(group, nameValuePair);
            }
            else
            {
                // Update the value for given group and name.
                this.rules[group][name] = value;
            }

            // Down times are handled here - other rules are handled by the monitor point.
            if (group == this.downtimeGroup && name == this.defaultName)
            {
                // Report that default downtime was set.
                Debug.Print("Set default downtime to: " + value);
            }
            else if (group == this.downtimeGroup)
            {
                // Report downtime for given provider.
                Debug.Print("Added rule, Provider: " + name + " - DownTime: " 
                    + value);
            }
        }

        /// <summary>
        /// Clears all of the group/name/value sets that have been added, 
        /// then readds the default downtime.
        /// </summary>
        public void Clear()
        {
            Debug.Print("Resetting rules");
            this.rules.Clear();
            this.SetDefaultDowntime();
        }

        /// <summary>
        /// Checks provider and default rules against the current downtime.
        /// </summary>
        /// <param name="providerId">ID of the provider of the current site 
        /// being checked.</param>
        /// <param name="downtime">Downtime of the current site being 
        /// checked.</param>
        /// <returns>Whether or not the given downtime is greater than the 
        /// max spec.</returns>
        public bool HasExcessDowntime(int providerId, int downtime)
        {
            // See if provider has a max downtime specified.  If they do, 
            // return comparison.
            string providerIDString = providerId.ToString(CultureInfo.InvariantCulture);
            if (this.rules[this.downtimeGroup][providerIDString] != null)
            {
                return downtime >= Convert.ToInt32(
                    this.rules[this.downtimeGroup][providerIDString], CultureInfo.InvariantCulture);
            }

            // Since provider didn't have a downtime, compare against the default
            if (downtime >= Convert.ToInt32(
                this.rules[this.downtimeGroup][this.defaultName], CultureInfo.InvariantCulture))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the value of a rule for a given group and name
        /// </summary>
        /// <param name="group">Name of the rule group</param>
        /// <param name="name">Name of the rule</param>
        /// <returns>Value of the specified rule</returns>
        public string GetRuleValue(string group, string name)
        {
            return this.rules[group][name];
        }

        /// <summary>
        /// Adds the default downtime to the rules object
        /// </summary>
        private void SetDefaultDowntime()
        {
            // Default downtime is 30 minutes unless overridden by the database
            Dictionary<string, string> nameValuePair = 
                new Dictionary<string, string>();
            nameValuePair.Add(this.defaultName, "30");
            this.rules.Add(this.downtimeGroup, nameValuePair);
        }
    }
}
