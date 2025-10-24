// <copyright file="TelnetLinkstar.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Monitoring.Linkstar
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Web;
    using MainstreamData.Data;
    using MainstreamData.Logging;
    using MainstreamData.Utility;
    using MainstreamData.Web;

    /// <summary>
    /// The type of Linkstar telnet host.
    /// </summary>
    public enum LinkstarHostType
    {
        /// <summary>
        /// The RNCC used to communicate with the RCSTs.
        /// </summary>
        Rncc,

        /// <summary>
        /// The NCC for controlling the network.
        /// </summary>
        Ncc
    }

    /// <summary>
    /// Provides an interface into a Linkstar host. Is based on TelnetRNCC Java class written by kporter.
    /// </summary>
    public abstract class TelnetLinkstar : IDisposable
    {
        /// <summary>
        /// The current region ID of the host.
        /// </summary>
        private string region = "Region not set.";

        /// <summary>
        /// Holds the Linkstar DB connection info.
        /// </summary>
        private SqlConnectionStringBuilder connectionStringBuilder;

        /// <summary>
        /// A value indicating whether the current instance of this class has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelnetLinkstar"/> class.
        /// </summary>
        /// <param name="connectionStringBuilder">The Linkstar DB connection info.</param>
        /// <param name="region">The region value.</param>
        /// <param name="type">The type of host to telnet to.</param>
        public TelnetLinkstar(SqlConnectionStringBuilder connectionStringBuilder, string region, LinkstarHostType type)
        {
            this.BufferBuilder = new StringBuilder();
            this.LockObject = new object();
            this.ClearBuffer();

            // Call setter to create DB object.
            this.ConnectionStringBuilder = connectionStringBuilder;

            // Store the type so that the correct IP address is retrieved.
            this.HostType = type;

            // Call setter to lookup ip address in DB.
            this.Region = region;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TelnetLinkstar"/> class.
        /// </summary>
        ~TelnetLinkstar()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets or sets the object containing the Linkstar DB connection info. Must set region after this for ip address to be updated.
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
                this.Database = new SqlDatabaseExtended(this.connectionStringBuilder);
            }
        }

        /// <summary>
        /// Gets the current contents of the buffer.
        /// </summary>
        /// <returns></returns>
        public string Buffer
        {
            get
            {
                lock (this.LockObject)
                {
                    return this.BufferBuilder.ToString();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current region.
        /// </summary>
        public string Region
        {
            get
            {
                return this.region;
            }

            set
            {
                this.region = value;
                this.ClearBuffer();

                // lookup the ip address
                this.IpAddress = this.GetIPAddress();
            }
        }

        /// <summary>
        /// Gets the type of host to telnet to.
        /// </summary>
        public LinkstarHostType HostType { get; private set; }

        /// <summary>
        /// Gets the IP address of the server to telnet to.
        /// </summary>
        public string IpAddress { get; private set; }

        /// <summary>
        /// Gets the object that to lock to provide thread safety.
        /// </summary>
        protected object LockObject { get; private set; }

        /// <summary>
        /// Gets the object used to perform telnet actions.
        /// </summary>
        protected TelnetClient Telnet { get; private set; }

        /// <summary>
        /// Gets the object for storing the data from recent telnet activity.
        /// </summary>
        protected StringBuilder BufferBuilder { get; private set; }

        /// <summary>
        /// Gets the object that provides database connectivity and functionality. Is intantiated in the ConnectionStringBuilder set method.
        /// </summary>
        protected SqlDatabaseExtended Database { get; private set; }

        /// <summary>
        /// Performs the specified action on the specified rcst.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="rcst">The rcst to perform the action on.</param>
        public abstract void Execute(string action, string rcst);

        /// <summary>
        /// Clears data from the buffer.
        /// </summary>
        public void ClearBuffer()
        {
            lock (this.LockObject)
            {
                this.BufferBuilder.Clear();
                this.BufferBuilder.AppendLine("Waiting for command");
            }
        }

        /// <summary>
        /// Opens the connection to the telnet host.
        /// </summary>
        public void Connect()
        {
            lock (this.LockObject)
            {
                // set variables
                ////buffer = "";

                if (string.IsNullOrEmpty(this.IpAddress))
                {
                    this.BufferBuilder.Clear();
                    const string Message = "Region not set!";
                    this.BufferBuilder.AppendLine(Message);
                    this.ThrowException(Message);
                }

                // create new Telnet instance
                this.Telnet = new TelnetClient(this.IpAddress, 23);

                // establish Telnet connection
                if (!this.Telnet.IsConnected)
                {
                    this.BufferBuilder.Clear();
                    string message = "Failed to connect to server on region " + this.region + " at ip address " + this.IpAddress;
                    this.BufferBuilder.AppendLine(message);
                    this.ThrowException(message);
                }
            }
        }

        /// <summary>
        /// Closes the telnet connection.
        /// </summary>
        public void Disconnect()
        {
            lock (this.LockObject)
            {
                // disconnect from server
                if (null != this.Telnet)
                {
                    this.Telnet.Dispose();
                }

                this.Telnet = null;
            }
        }

        /// <summary>
        /// Cleans up resources used by this class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Adds 0x to termId if needed.
        /// </summary>
        /// <param name="termId">The termId to fix.</param>
        protected static void FixTermId(ref string termId)
        {
            if (!termId.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                termId = "0x" + termId;
            }
        }

        /// <summary>
        /// Cleans up resources used by this class.
        /// </summary>
        /// <param name="disposing">True if cleaning up managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                lock (this.LockObject)
                {
                    if (disposing)
                    {
                        if (this.Telnet != null)
                        {
                            this.Telnet.Dispose();
                        }
                    }

                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Adds server information to the message then throws a WebException.
        /// </summary>
        /// <param name="message">The message to include.</param>
        protected void ThrowException(string message)
        {
            message = string.Format(CultureInfo.InvariantCulture, "Failed to connect to host {0}. {1}", this.IpAddress, message);
            throw new WebException(message);
        }

        /// <summary>
        /// Trys to connect to the host.
        /// </summary>
        /// <returns>True if was able to connect.</returns>
        protected bool TryConnect()
        {
            bool connected = false;
            try
            {
                this.Connect();
                connected = true;
            }
            catch (WebException ex)
            {
                ExtendedLogger.WriteException("Unable to connect to " + this.HostType + " at: " + this.region, Category.Config, Priority.High, ex);
            }

            return connected;
        }

        /// <summary>
        /// Trys to write to the host and waits to receive back a specific string.
        /// </summary>
        /// <param name="command">The command to write.</param>
        /// <param name="waitFor">The string to wait for.</param>
        /// <returns>True if didn't timeout.</returns>
        protected bool TryWriteAndWait(string command, string waitFor)
        {
            bool result = true;
            try
            {
                this.BufferBuilder.Append(this.Telnet.WriteAndWait(command, waitFor));
            }
            catch (TimeoutException ex)
            {
                ExtendedLogger.WriteException("Timout occurred before string '" + waitFor + "' was found.", Category.Config, Priority.High, ex);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Updates the buffer in this class then clears the telnet buffer.
        /// </summary>
        protected void UpdateBufferThenClear()
        {
            // Encode the telnet data for display on a web page.
            this.BufferBuilder.Append(HttpUtility.HtmlEncode(this.Telnet.Buffer));
            this.Telnet.ClearBuffer();
        }

        /// <summary>
        /// Performs the login to the host and updates the buffer accordingly.
        /// </summary>
        /// <param name="doRT">A value indicating whether or not to execute the rttelnet command after login.</param>
        /// <returns>The telnet responses for the login.</returns>
        protected virtual int Login(bool doRT)
        {
            int commandResult = 1;
            try
            {
                // TODO: Move user and password to DB or config file.
                string username = "lsviasat";
                string password = "viasat";
                if (this.HostType == LinkstarHostType.Ncc)
                {
                    username = "linkstar";
                }

                this.Telnet.Wait("login:", 10);
                this.Telnet.WriteAndWait(username, "Password:");
                this.Telnet.WriteAndWait(password, new string[] { "lsviasat%ncc2-1", "lsviasat%rncc2-1", "lsviasat%ncc1-1", "linkstar%ncc2-1" });
                this.Telnet.ClearBuffer();
                if (true == doRT)
                {
                    this.Telnet.WriteAndWait("rttelnet 1e", "cmlabs");
                    this.Telnet.ClearBuffer();
                }
            }
            catch (TimeoutException ex)
            {
                this.BufferBuilder.AppendLine();
                this.BufferBuilder.AppendLine(ex.ToString());
                commandResult = 0;
            }
            finally
            {
                this.UpdateBufferThenClear();
            }

            return commandResult;
        }

        /// <summary>
        /// Sends a command to the host.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <returns>1 if successful, otherwise zero.</returns>
        protected int SendCommand(string command)
        {
            int commandResult = 1;
            this.Telnet.WriteLine(command);

            // Give the command time to succeed - just a bit over 1 second to allow cw to stop
            string results = string.Empty;
            ////for (int i = 0; i > 3; i++)
            {
                results = this.Telnet.Read(1100);
                ////if (!string.IsNullOrEmpty(results))
                {
                    ////break;
                }
            }

            if (string.IsNullOrEmpty(results))
            {
                this.BufferBuilder.AppendLine();
                this.BufferBuilder.AppendLine("No data was received from the telnet client.");
                commandResult = 0;
            }

            this.UpdateBufferThenClear();
            return commandResult;
        }

        /// <summary>
        /// Logs into host (and sends rttelnet command) then sends the specified command.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <returns>1 if successful, otherwise zero.</returns>
        protected int LoginAndSendCommand(string command)
        {
            return this.LoginAndSendCommand(command, true);
        }

        /// <summary>
        /// Logs into host then sends the specified command.
        /// </summary>
        /// <param name="command">The command to send.</param>
        /// <param name="doRT">A value indicating whether or not to execute the rttelnet command after login.</param>
        /// <returns>1 if successful, otherwise zero.</returns>
        protected int LoginAndSendCommand(string command, bool doRT)
        {
            int commandResult = this.Login(doRT);
            if (commandResult == 1)
            {
                commandResult = this.SendCommand(command);
            }

            return commandResult;
        }

        /// <summary>
        /// Gets the IP address for the specified host type.
        /// </summary>
        /// <returns>The specified IP address.</returns>
        private string GetIPAddress()
        {
            return this.GetIPAddress(this.HostType);
        }

        /// <summary>
        /// Gets the IP address for the specified host type.
        /// </summary>
        /// <param name="hostType">The type of host to get the IP address for.</param>
        /// <returns>The specified IP address.</returns>
        private string GetIPAddress(LinkstarHostType hostType)
        {
            string name = hostType.ToString().ToUpper();

            string procedure = "sel" + name + "IPAddress";
            string tempIPAddress = null;
            try
            {
                using (IDataReader reader = this.Database.ExecuteReader(procedure, this.Region))
                {
                    while (reader.Read())
                    {
                        tempIPAddress = reader["ipAddress"].ToNullable<string>();
                        break;
                    }
                }
            }
            catch (SqlException ex)
            {
                string message = "Error attempting to get " + name + " IP address from database for " + name + " " + this.Region;
                ExtendedLogger.WriteException(message, Category.Config, Priority.High, ex);
                this.BufferBuilder.AppendLine(message);
            }

            if (tempIPAddress == null)
            {
                this.BufferBuilder.Clear();
                this.BufferBuilder.AppendLine("Unable to retrieve IP Address");
            }

            return tempIPAddress;
        }
    }
}