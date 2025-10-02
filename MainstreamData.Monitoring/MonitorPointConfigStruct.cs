// -----------------------------------------------------------------------
// <copyright file="MonitorPointConfigStruct.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>
// -----------------------------------------------------------------------

namespace MainstreamData.Monitoring
{
    /// <summary>
    /// Simple struct for holding monitor point configuration data.
    /// </summary>
    public struct MonitorPointConfigStruct
    {
        /// <summary>
        /// Gets or sets display name shown in HAL interface.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets location of monitor point - see MonitorLocation table.
        /// </summary>
        public int LocationId { get; set; }

        /// <summary>
        /// Gets or sets connection name. Not sure what this is used for, but is stored into MonitorPoint table.
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        /// Gets or sets name or IP of server where data is stored that is specific to this monitor point.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets user ID for logging into DB - see server field.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Gets or sets password for logging into DB - see server field.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets database where information is stored for the current monitor point.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets the type value stored in the Smartcode table that relates to this monitor point.
        /// </summary>
        public string SmartcodeType { get; set; }

        /// <summary>
        /// Gets or sets the typeCode value stored in the Smartcode table that relates to this monitor point.
        /// </summary>
        public string SmartcodeTypeCode { get; set; }

        /// <summary>
        /// Gets or sets the Smartcode description for this monitor point.
        /// </summary>
        public string SmartcodeDesc { get; set; }
    }
}
