// <copyright file="HalController.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring
{
    using System.Collections.Generic;
    using System.Data;              // Used to access IDataReader
    using System.Data.Common;       // Used to access DBCommand
    using System.Data.SqlClient;    // Used to access SqlConnectionStringBuilder
    using System.Globalization;
    using System.Text;
    using MainstreamData.Data;      // Used for SqlDatabaseExtended
    using MainstreamData.ExceptionHandling;
    using MainstreamData.Logging;   // Used for EventLogHandler

    /// <summary>
    /// Connects to the HAL database to retrieve parameters and update status
    /// </summary>
    public class HalController
    {
        // TODO: Review properties to see if they need to be visible outside this class or can be made private.

        /// <summary>
        /// Declare database reference to be used in multiple methods - is 
        /// instantiated in RetrieveData method
        /// </summary>
        private SqlDatabaseExtended database;

        /// <summary>
        /// Server ID in HAL for this monitor point.  Allows multiple instances
        /// of any given monitor point (i.e. it is unique across a single monitor
        /// point type).
        /// </summary>
        private int serverKeyId;

        /// <summary>
        /// Stores the ID for this monitor point instance - retrieved by looking up 
        /// serverKeyID and typeCode
        /// </summary>
        private int? monitorPointId = 0;

        /// <summary>
        /// Rules (aka SiteParameters) for gauging downtime, etc.
        /// </summary>
        private MPRuleHandler alertRules = new MPRuleHandler();

        /// <summary>
        /// Holds the connection string for the Hal database connection.
        /// </summary>
        private SqlConnectionStringBuilder connectionStringBuilder;

        /// <summary>
        /// Holds configuration data for the monitor point.
        /// </summary>
        private MonitorPointConfigStruct monitorPointConfig = new MonitorPointConfigStruct();

        /// <summary>
        /// Holds default alert typecodes
        /// </summary>
        private Dictionary<string, string> alertTypeCodes = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the HalController class.
        /// </summary>
        /// <param name="defaultSmartcodeType">The type value for the smartcode database table
        /// (e.g. MP = monitor point).</param>
        /// <param name="defaultSmartcodeTypeCode">The type code used for the monitor 
        /// point (e.g. MW = medias website monitor).</param>
        /// <param name="defaultSmartcodeDesc">The description for the typecode
        /// (e.g. "Medias Website")</param>
        /// <param name="defaultDisplayName">Name for HAL to display for this monitor point.</param>
        /// <param name="defaultLocationId">Location where monitor point is running.</param>
        /// <param name="defaultConnectionName">Not used here, but is stored in MonitorPoint table.</param>
        /// <param name="defaultServer">Server where monitor point database is stored.</param>
        /// <param name="defaultUser">Login name for monitor point database.</param>
        /// <param name="defaultPassword">Password for monitor point database.</param>
        /// <param name="defaultDatabase">Monitor point database name.</param>
        public HalController(
            string defaultSmartcodeType, 
            string defaultSmartcodeTypeCode, 
            string defaultSmartcodeDesc,
            string defaultDisplayName, 
            int defaultLocationId, 
            string defaultConnectionName,
            string defaultServer, 
            string defaultUser, 
            string defaultPassword, 
            string defaultDatabase)
        {
            this.monitorPointConfig.SmartcodeType = defaultSmartcodeType;
            this.monitorPointConfig.SmartcodeTypeCode = defaultSmartcodeTypeCode;
            this.monitorPointConfig.SmartcodeDesc = defaultSmartcodeDesc;
            this.monitorPointConfig.DisplayName = defaultDisplayName;
            this.monitorPointConfig.LocationId = defaultLocationId;
            this.monitorPointConfig.ConnectionName = defaultConnectionName;
            this.monitorPointConfig.Server = defaultServer;
            this.monitorPointConfig.User = defaultUser;
            this.monitorPointConfig.Password = defaultPassword;
            this.monitorPointConfig.Database = defaultDatabase;
        }

        /// <summary>
        /// Gets MonitorPointConfig object for retrieving monitor point config settings.
        /// </summary>
        public MonitorPointConfigStruct MonitorPointConfig
        {
            get 
            { 
                return this.monitorPointConfig; 
            }
        }

        /// <summary>
        /// Gets the ID from the database for this monitor point.
        /// </summary>
        public int? MonitorPointId
        {
            get
            {
                return this.monitorPointId;
            }
        }

        /// <summary>
        /// Gets or sets the server ID used by HAL for this monitor point.
        /// </summary>
        public int ServerKeyId
        {
            get
            {
                return this.serverKeyId;
            }

            set
            {
                this.serverKeyId = value;
            }
        }

        /// <summary>
        /// Gets rules (aka SiteParameters) object containing rules for gauging 
        /// downtime, etc.
        /// </summary>
        public MPRuleHandler AlertRules
        {
            get
            {
                return this.alertRules;
            }
        }

        /// <summary>
        /// Gets or sets the object containing the hal DB connection info.
        /// </summary>
        public SqlConnectionStringBuilder ConnectionStringBuilder
        {
            get 
            { 
                return this.connectionStringBuilder; 
            }

            set 
            {
                this.connectionStringBuilder = value;

                // Database doesn't have dispose method, so can just overwrite old copy at will.
                this.database = new SqlDatabaseExtended(this.connectionStringBuilder);
            }
        }

        /// <summary>
        /// See if the site is on the exceptions list - If it is, don't send 
        /// an alert.  In case of database errors, we assume it's not on the list.
        /// </summary>
        /// <param name="siteId">The site ID used by HAL to identify a single 
        /// site</param>
        /// <returns>True if the given siteID is in the exceptions list</returns>
        public bool IsInExceptionList(long siteId)
        {
            // Build command to check exception list.
            bool result = false; 
            try
            {
                using (DbCommand cmd = this.database.GetStoredProcCommand(
                    "spIsOnExceptionList", this.monitorPointId, siteId))
                {
                    result = (bool)this.database.ExecuteScalar(cmd);
                }
            }
            catch (SqlException ex)
            {
                string message = "Error retrieving alert exception rules.";
                ExtendedLogger.WriteException(message, Category.Exception, Priority.High, ex);
                throw new HalException(message, ex);
            }

            return result;
        }

        /// <summary>
        /// Prompts HAL to send an alert
        /// </summary>
        /// <param name="description">Description to include in the alert</param>
        /// <param name="priority">Priority of the alert</param>
        /// <param name="ownerId">Owner ID for the individual associated with the 
        /// alert.</param>
        /// <param name="alertTypeCode">The typeCode for a given alert - see 
        /// SmartCode table for details.</param>
        /// <param name="url">A URL to be included within the alert email??</param>
        public void SendAlert(
            string description, 
            int priority, 
            int ownerId, 
            string alertTypeCode, 
            string url)
        {
            // Build command to send alert
            try
            {
                using (DbCommand cmd = this.database.GetStoredProcCommand(
                    "insNewAlarm", 
                    this.monitorPointId, 
                    description, 
                    priority,
                    ownerId, 
                    alertTypeCode, 
                    url))
                {
                    int returnValue = this.database.ExecuteNonQuery(cmd);
                    //// TODO: Figure out why return value is -1 even though record got created.
                    if (returnValue != 0 && returnValue != -1)
                    {
                        string message = ExtendedLogger.AddErrorNumberToMessage(
                            "insNewAlarm stored procedure failed to send alert", returnValue);
                        ExtendedLogger.Write(message, Category.General, Priority.High);
                        throw new HalException(message);
                    }
                }
            }
            catch (SqlException ex)
            {
                string message = "Failed to send alert.";
                ExtendedLogger.WriteException(message, Category.General, Priority.High, ex);
                throw new HalException(message, ex);
            }

            return;
        }

        /// <summary>
        /// Connect to hal database and retrieve the monitor point configuration.
        /// You must set the ServerKeyID and the ConnectionStringBuilder properties
        /// before calling this method.
        /// </summary>
        public void GetMonitorPointData()
        {
            // Build command to get monitor point id
            try
            {
                // Get config params
                if (!this.GetMonitorPointID())
                {
                    this.CreateMonitorPoint();
                }
            }
            catch (SqlException ex)
            {
                string message = "Error retrieving monitor point information.";
                ExtendedLogger.WriteException(message, Category.Config, Priority.High, ex);
                throw new ConfigurationException(message, ex);
            }

            // Build command to get site parameters
            try
            {
                using (DbCommand cmd = this.database.GetStoredProcCommand(
                "selSiteParameters", this.monitorPointId))
                {
                    // Execute the command to get site parameters
                    using (IDataReader reader = this.database.ExecuteReader(cmd))
                    {
                        // Store the rules in the rules object
                        while (reader.Read())
                        {
                            this.alertRules.Add(
                                reader["group"].ToString(),
                                reader["name"].ToString(),
                                reader["value"].ToString());
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                string message = "Error retrieving alert rules.";
                ExtendedLogger.WriteException(message, Category.Config, Priority.High, ex);
                throw new ConfigurationException(message, ex);
            }
        }

        /// <summary>
        /// Retrieves the monitor point ID from the database.
        /// </summary>
        /// <returns>The monitor point ID.</returns>
        private bool GetMonitorPointID()
        {
            // Errors currently handled by caller.
            // TODO: Use returnValue to determine course of action.
            bool monitorPointFound = false;
            using (DbCommand cmd = this.database.GetStoredProcCommand(
                "selMonitorPoint", this.serverKeyId, this.monitorPointConfig.SmartcodeTypeCode))
            {
                this.monitorPointId = (int?)this.database.ExecuteScalar(cmd);
                monitorPointFound = this.monitorPointId.HasValue;
            }

            return monitorPointFound;
        }

        /// <summary>
        /// Creates the monitor point in the database using the default values.
        /// </summary>
        private void CreateMonitorPoint()
        {
            MonitorPointConfigStruct config = this.monitorPointConfig;

            // See if smartcode exists.
            bool smartcodeFound = false;
            string sql = "SELECT type, typeCode, description FROM SmartCode WHERE " +
                "type = '{0}' AND typeCode = '{1}'";
            using (DbCommand cmd = this.database.GetSqlStringCommand(
                string.Format(CultureInfo.InvariantCulture, sql, config.SmartcodeType, config.SmartcodeTypeCode)))
            {
                // Execute the command to get the record
                using (IDataReader reader = this.database.ExecuteReader(cmd))
                {
                    smartcodeFound = reader.Read();
                }
            }

            // Create smartcode if needed.
            if (!smartcodeFound)
            {
                sql = "INSERT INTO SmartCode (type, typeCode, Description) VALUES " +
                    "('{0}', '{1}', '{2}');";
                StringBuilder sqlInserts = new StringBuilder();
                sqlInserts.AppendLine(string.Format(CultureInfo.InvariantCulture, sql, config.SmartcodeType, config.SmartcodeTypeCode, config.SmartcodeDesc));
                foreach (KeyValuePair<string, string> alertTypeCode in this.alertTypeCodes)
                {
                    sqlInserts.AppendLine(string.Format(CultureInfo.InvariantCulture, sql, "AL", alertTypeCode.Key, alertTypeCode.Value));
                }

                using (DbCommand cmd = this.database.GetSqlStringCommand(sqlInserts.ToString()))
                {
                    this.database.ExecuteNonQuery(cmd);
                }
            }

            // Create monitor point
            sql = "INSERT INTO MonitorPoint (displayName, locationId, connectionName, " +
                "serverKeyId, server, [user], password, [database], typeCode, statusCall)" +
                " VALUES ('{0}', {1}, '{2}', {3}, '{4}', '{5}', '{6}', '{7}', '{8}', '{9}')";
            using (DbCommand cmd = this.database.GetSqlStringCommand(
                string.Format(
                    CultureInfo.InvariantCulture,
                    sql,
                    config.DisplayName,
                    config.LocationId,
                    config.ConnectionName,
                    this.serverKeyId,
                    config.Server,
                    config.User,
                    config.Password,
                    config.Database,
                    config.SmartcodeTypeCode,
                    "reqHALStatus")))
            {
                this.database.ExecuteNonQuery(cmd);
            }

            // Log creation
            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Added iServerKeyID {0} to {1} database on {2}",
                this.serverKeyId,
                this.monitorPointConfig.Database,
                this.monitorPointConfig.Server);
            ExtendedLogger.Write(message, Category.Config, Priority.Low);
        }
    }
}
