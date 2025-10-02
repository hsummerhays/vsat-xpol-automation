// <copyright file="ConeheadController.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;    // Used for catching Win32Exception
    using System.Data;              // Used to access IDataReader
    using System.Data.Common;       // Used to access DBCommand
    using System.Data.SqlClient;    // Used to access SqlConnectionStringBuilder
    using System.Globalization;
    using System.Timers;
    using MainstreamData.Data;      // Used for SqlDatabaseExtended
    using MainstreamData.ExceptionHandling;
    using MainstreamData.Logging;
    using MainstreamData.Utility;      // Used for ApplicationInfo

    // TODO: Decided if want to replace HalException with something more specific for this class.

    /// <summary>
    /// Connects to the Conehead database to retrieve parameters 
    /// and update status.  This is the C# equivalent of the Mainstream 
    /// C++ cdop, clam, and codbc classes, but for C# monitor points.
    /// This class does not support Conehead ConfigProc table commands 
    /// since it only checks in every 24 hours.
    /// </summary>
    public class ConeheadController : IDisposable
    {
        // TODO: Review properties to see if they need to be visible outside this class or can be made private.

        /// <summary>
        /// Field level variable for storing the programName, with it 
        /// formatted as applicationName_machineName.
        /// </summary>
        private readonly string programName = ApplicationInfo.Name + "_" + 
            ComputerInfo.MachineName;

        /// <summary>
        /// Declare database reference to be used in multiple methods - 
        /// is instantiated in RetrieveData method
        /// </summary>
        private SqlDatabaseExtended database;

        /// <summary>
        /// Stores databaseServer name so it can be used outside the class
        /// </summary>
        private string databaseServer = string.Empty;

        /// <summary>
        /// Field level variable for storing the ProgramID provided by the 
        /// spGetProgID stored procedure. Not really used since other stored 
        /// procedures require the programName.
        /// </summary>
        private int? programId = null;

        /// <summary>
        /// An array for storing configuration parameters retrieved from 
        /// the database
        /// </summary>
        private Dictionary<string, string> configParams = 
            new Dictionary<string, string>();

        /// <summary>
        /// A place to store paramaters for HAL connection.
        /// </summary>
        private Dictionary<string, string> defaultHalParams = new Dictionary<string, string>();

        /// <summary>
        /// A time for updating the ConfigProc table's LastHeardFrom date 
        /// every 24 hours
        /// </summary>
        private Timer timer = new Timer(60 * 60 * 24 * 1000);

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the ConeheadController class.
        /// </summary>
        public ConeheadController()
        {
            // Wire up the timer's elapsed event
            this.timer.Elapsed += new ElapsedEventHandler(this.Timer_Elapsed);
        }
        
        /// <summary>
        /// Finalizes an instance of the ConeheadController class.  Exists to avoid cleaning up items too soon.
        /// </summary>
        ~ConeheadController()
        {
            this.Dispose(false);
        }
        
        /// <summary>
        /// Gets the database server name provided to this class 
        /// via the ini file.
        /// </summary>
        public string DatabaseServer
        {
            get { return this.databaseServer; }
        }

        /// <summary>
        /// Gets the current programID provided by the spGetProgID stored 
        /// procedure. Not really used since other stored procedures 
        /// require the programName.
        /// </summary>
        public int? ProgramId
        {
            get { return this.programId; }
        }

        /// <summary>
        /// Gets the current ProgramName being used in the database
        /// </summary>
        public string ProgramName
        {
            get { return this.programName; }
        }

        /// <summary>
        /// Gets a list of configuration parameters that can be referenced by 
        /// name (e.g. ConfigParams["MyParam"] will return the MyParam value).
        /// </summary>
        public Dictionary<string, string> ConfigParams
        {
            get { return this.configParams; }
        }

        /// <summary>
        /// Retrieves login information from the ini file, then retrieves 
        /// configuration data from the database.  The stored procedures 
        /// use will automatically add the program if it doesn't already exist.
        /// </summary>
        public void RetrieveData()
        {
            // Use ini file values to setup database - it doesn't attempt to connect yet.
            this.database = new SqlDatabaseExtended(this.GetDbConnectionString());

            try
            {
                // Build command to get host ID info from this.database - will attempt to connect to DB.
                using (DbCommand cmd = this.database.GetStoredProcCommand("sp_GetHostId", ComputerInfo.MachineName))
                {
                    // Retrieve host ID from database - configuration error if unable
                    int hostId = (int)this.database.ExecuteScalar(cmd);

                    // Build command to get program ID from this.database.
                    using (DbCommand cmd2 = this.database.GetStoredProcCommand(
                        "sp_GetProgId", 
                        this.programName, 
                        hostId, 
                        ApplicationInfo.Name, 
                        "created by c# mp"))
                    {
                        // Retrieve program ID from this.database.
                        this.programId = (int)this.database.ExecuteScalar(cmd2);
                    }
                }

                // Get config params
                if (!this.GetConfigParams())
                {
                    this.InsertDefaultConfigParams();
                }
            }
            catch (SqlException ex)
            {
                string message = "Unable to retrieve program information from Conehead.";
                ExtendedLogger.WriteException(message, Category.Config, Priority.High, ex);
                throw new ConfigurationException(message, ex);
            }

            // Start the timer if it hasn't been already
            if (this.timer.Enabled == false)
            {
                this.timer.Enabled = true;
            }
        }

        /// <summary>
        /// Cleans up resources to avoid having to wait for garbage collection
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // Prevent garbage collection from calling finalize again since we already cleaned up.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check disposed field so Dispose does not get done more than once
            ////try
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        // Clean up managed resources
                        this.timer.Dispose();
                    }

                    // Clean up unmanaged resources (if any)
                }

                this.disposed = true;
            }
            ////finally
            {
                ////base.dispose(disposing);
            }
        }

        /// <summary>
        /// Retrieves configuration parameters from ConfigProcParam table in conehead DB.
        /// </summary>
        /// <returns>True if parameters were retrieved.</returns>
        private bool GetConfigParams()
        {
            // Errors currently being caught in calling code.
            bool foundParams = false;

            // Build command to get configuration parameters from this.database.
            using (DbCommand cmd = this.database.GetStoredProcCommand(
                "sp_GetConfigProcParam", this.programName))
            {
                 // Retrieve configuration parameters from this.database
                using (IDataReader reader = this.database.ExecuteReader(cmd))
                {
                    while (reader.Read())
                    {
                        string key1 = reader[0].ToString();
                        //// configReader[1] is the KEY2 field, which is not currently used.
                        string value = reader[2].ToString();
                        if (this.configParams.ContainsKey(key1))
                        {
                            this.configParams[key1] = value;
                        }
                        else
                        {
                            this.configParams.Add(key1, value);
                        }

                        foundParams = true;
                    }
                }
            }

            return foundParams;
        }

        /// <summary>
        /// Inserts default configuration parameters into ConfigProcParam table in conehead DB.
        /// </summary>
        private void InsertDefaultConfigParams()
        {
            if (this.defaultHalParams.Count == 0)
            {
                return;
            }

            // Errors currently being caught in calling code.
            string rawInsert = "INSERT INTO CONFIGPROCPARAM (ProcName,KEY1,KEY2,VALUE) VALUES ('{0}','{1}',null,'{2}')";

            foreach (KeyValuePair<string, string> param in this.defaultHalParams)
            {
                string insert = string.Format(CultureInfo.InstalledUICulture, rawInsert, this.programName, param.Key, param.Value);
                using (DbCommand cmd = this.database.GetSqlStringCommand(insert))
                {
                    this.database.ExecuteNonQuery(cmd);
                }
            }

            // Log creation
            string message = string.Format(
                CultureInfo.InvariantCulture,
                "Added ConfigProcParams for {0} in {1} database on {2}.",
                this.programName,
                "conehead",
                this.databaseServer);
            ExtendedLogger.Write(message, Category.Config, Priority.Low);

            this.GetConfigParams();
        }

        /// <summary>
        /// Method for handling the timer that updates the LastHeardFrom date 
        /// in the ConfigProc table.
        /// </summary>
        /// <param name="sender">The object that called the method 
        /// via an event.</param>
        /// <param name="e">Contains any arguments provided by the event.</param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //// TODO: See about using System.Threading.Timer instead, but make sure is used in thread safe manner.
            // System.Timers.Timer swallows exceptions - log any that happen here.
            try
            {
                // Build command to update LastHeardFrom
                using (DbCommand cmd = this.database.GetStoredProcCommand(
                    "sp_UpdateLastHeard", this.programName, "running"))
                {
                    // Update last heard and ignore the returned command
                    int result = this.database.ExecuteNonQuery(cmd);
                    if (result > 2)
                    {
                        // Found that 2 is an OK return value for sp_updatelastheard - not really sure about 1 though
                        string message = ExtendedLogger.AddErrorNumberToMessage(
                            "Error running sp_UpdateLastHeard in Conehead DB", result);
                        ExtendedLogger.Write(message, Category.General, Priority.High);
                        //// throw new HalException(message);    // This won't be caught using System.Timers.Timer.
                    }
                }
            }
            catch (Exception ex)
            {
                string message = "Unhandled exception detected in ConeheadController timer thread.";
                ExtendedLogger.WriteException(message, Category.General, Priority.High, ex);
                throw;
            }
        }

        /// <summary>
        /// Reads values from the ini configuration file
        /// </summary>
        /// <returns>Returns a connection string that contains the login 
        /// information retrieved from the ini file.</returns>
        private SqlConnectionStringBuilder GetDbConnectionString()
        {
            SqlConnectionStringBuilder conStringBuilder = 
                new SqlConnectionStringBuilder();

            string section = "ConfigurationDB";

            try
            {
                IniFile iniFile = new IniFile();
                iniFile.Load();
                conStringBuilder.DataSource = iniFile[section]["Server"];

                // Make the databaseServer value available outside this class
                this.databaseServer = conStringBuilder.DataSource;

                // Read additional values from ini file
                conStringBuilder.InitialCatalog = iniFile[section]["Database"];
                conStringBuilder.UserID = iniFile[section]["User"];
                conStringBuilder.Password = iniFile[section]["Password"];

                //// TODO: Allow program to continue running even if these values don't exist since they may not be necessary to continue.
                // Read HalDefaults
                this.defaultHalParams = new Dictionary<string, string>();
                section = "HalDefaults";
                string key = "HalServer";
                this.defaultHalParams.Add(key, iniFile[section][key]);  // e.g. hal.mainstreamdata.com
                key = "HalDatabase";
                this.defaultHalParams.Add(key, iniFile[section][key]);  // e.g. hal
                key = "HalUser";
                this.defaultHalParams.Add(key, iniFile[section][key]);  // e.g. haluser
                key = "HalPassword";
                this.defaultHalParams.Add(key, iniFile[section][key]);
                key = "ServerKey";
                this.defaultHalParams.Add(key, iniFile[section][key]);  // e.g. 1
            }
            catch (ConfigurationException ex)
            {
                ExtendedLogger.WriteException(string.Empty, Category.Config, Priority.High, ex);
                throw;
            }
            catch (KeyNotFoundException ex)
            {
                string message = "Error reading values from ini file.";
                ExtendedLogger.WriteException(message, Category.Config, Priority.High, ex);
                throw new ConfigurationException(message, ex);
            }

            return conStringBuilder;
        }
    }
}
