// <copyright file="TelnetRncc.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

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
    /// Provides an interface into the RNCC. Is based on Java class written by kporter.
    /// </summary>
    public class TelnetRncc : TelnetLinkstar
    {
        /// <summary>
        /// Provides connection to NCC if needed.
        /// </summary>
        private TelnetNcc ncc = null;

        /// <summary>
        /// A value indicating whether the current instance of this class has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelnetRncc"/> class.
        /// </summary>
        /// <param name="connectionStringBuilder">The Linkstar DB connection info.</param>
        /// <param name="region">The RNCC region value.</param>
        public TelnetRncc(SqlConnectionStringBuilder connectionStringBuilder, string region)
            : base(connectionStringBuilder, region, LinkstarHostType.Rncc)
        {
            this.ncc = new TelnetNcc(connectionStringBuilder, region);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the RNCC will to attempt to put the RCST in receive only mode when sending up a clear wave.
        /// </summary>
        public bool NetworkSupportsReceiveOnly { get; set; }

        /// <summary>
        /// Performs the specified action on the specified rcst.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="rcst">The rcst to perform the action on.</param>
        public override void Execute(string action, string rcst)
        {
            lock (this.LockObject)
            {
                TelnetRncc.FixTermId(ref rcst);
                if (this.TryConnect())
                {
                    action = action.ToUpperInvariant();
                    switch (action)
                    {
                        case "DT":
                            // Disable terminal
                            this.LoginAndSendCommand("disableterm " + rcst);
                            break;
                        case "ET":
                            // Enable terminal
                            this.LoginAndSendCommand("enableterm " + rcst);
                            break;
                        case "REBOOT":
                            this.LoginAndSendCommand("sendutermcmd " + rcst + " hw");
                            break;
                        case "SHOW":
                            // Clear out the buffer since this returns a lot
                            this.BufferBuilder.Clear();
                            this.LoginAndSendCommand("showrcst " + rcst);
                            break;
                        default:
                            this.BufferBuilder.AppendLine(action + " is not a valid action.");
                            break;
                    }

                    this.Disconnect();
                }
            }
        }

        /// <summary>
        /// Initiates or terminates a clear wave using the specified parameters.
        /// </summary>
        /// <param name="action">Initialize (with icw) or Terminate (with tcw).</param>
        /// <param name="rcst">The termId of the RCST.</param>
        /// <param name="power">The power level in dBm.</param>
        /// <param name="freq">The frequency in ?</param>
        /// <param name="time">The time in seconds to keep the clearwave up.</param>
        public void ClearWave(string action, string rcst, string power, string freq, string time)
        {
            lock (this.LockObject)
            {
                const string MessageStart = "Clearwave Command: ";
                bool isInitiate = action.Equals("ICW", StringComparison.OrdinalIgnoreCase); // terminate is "tcw"

                TelnetRncc.FixTermId(ref rcst);
                if (this.TryConnect() && this.CheckNccIPAddress())
                {
                    int commandResult = this.Login(true);
                    if (commandResult == 1)
                    {
                        string command = string.Empty;
                        if (isInitiate)
                        {
                            // Disable the terminal first
                            if (this.NetworkSupportsReceiveOnly)
                            {
                                // Put into receive only mode.
                                ExtendedLogger.Write("Executing settermadmin receive only (3) for rcst " + rcst, Category.General, Priority.Low);
                                this.ncc.ClearBuffer();
                                this.ncc.Execute("RO", rcst);
                                this.BufferBuilder.Append(this.ncc.Buffer);
                                Thread.Sleep(500);  // Give time for the RCST to go into receive only mode before moving onto other commands.
                            }
                            else
                            {
                                command = "disableterm " + rcst;
                                ExtendedLogger.Write(MessageStart + command, Category.General, Priority.Low);
                                commandResult = this.SendCommand(command);
                            }
                        }

                        if (commandResult == 1)
                        {
                            command = "sendutermcmd " + rcst + " cacsetcw" + " -power " + power + " -freq " + freq;

                            // If we are terminating the clearwave - we set the timeout
                            if (!isInitiate)
                            {
                                time = "1";    
                            }
                    
                            if (time.Length > 0)
                            {
                                command += " -time " + time;
                            }
                     
                            ExtendedLogger.Write(MessageStart + command, Category.General, Priority.Low);
                            this.SendCommand(command);
                        }

                        if (!isInitiate)
                        {
                            if (this.NetworkSupportsReceiveOnly)
                            {
                                // Take back out of receive only mode.
                                // TODO: See about storing original mode and setting back to that.
                                ExtendedLogger.Write("Executing settermadmin RX - TX Optional (2) for rcst " + rcst, Category.General, Priority.Low);
                                this.ncc.ClearBuffer();
                                this.ncc.Execute("RTO", rcst);
                                this.BufferBuilder.Append(this.ncc.Buffer);
                            }
                            else
                            {
                                // re-enable the terminal
                                command = "enableterm " + rcst;
                                ExtendedLogger.Write(MessageStart + command, Category.General, Priority.Low);
                                this.SendCommand(command);
                            }
                        }                
                    }
                    else
                    {
                        string message = "Failed to successfully contact RNCC.";
                        this.BufferBuilder.AppendLine();
                        this.BufferBuilder.AppendLine(message);
                        ExtendedLogger.Write(message, Category.Config, Priority.High);
                    }

                    this.Disconnect();
                }
            }
        }

        /// <summary>
        /// Moves the RCST to a new inbound carrier and/or population.
        /// </summary>
        /// <param name="rcst">The rcst to move.</param>
        /// <param name="carrier">The destination carrier.</param>
        /// <param name="population">The destination population.</param>
        public void MoveCarrier(string rcst, string carrier, string population)
        {
            this.MoveCarrier(rcst, carrier, population, false);
        }
                                            
        /// <summary>
        /// Moves the RCST to a new inbound carrier and/or population.
        /// </summary>
        /// <param name="rcst">The rcst to move.</param>
        /// <param name="carrier">The destination carrier.</param>
        /// <param name="population">The destination population.</param>
        /// <param name="removeFromPenaltyBox">Set to true if want to remove from penalty box.</param>
        public void MoveCarrier(string rcst, string carrier, string population, bool removeFromPenaltyBox)
        {
            lock (this.LockObject)
            {
                this.BufferBuilder.Clear();
                this.BufferBuilder.AppendLine("Moving RCST to new carrier.");
        
                if (!this.CheckNccIPAddress())
                {
                    return;
                }

                this.BufferBuilder.AppendLine("Terminating command! with NCC set to " + this.ncc.IpAddress);
                Thread.Sleep(3000);
        
                if (string.IsNullOrEmpty(rcst) || rcst.Length < 4)
                {
                    this.BufferBuilder.AppendLine("RCST was not set! Ignoring call to move carrier.");
                    return;    
                }
            
                TelnetRncc.FixTermId(ref rcst);
        
                // strip off any leading 0s
                while (rcst.StartsWith("0", StringComparison.OrdinalIgnoreCase))
                {    
                    rcst = rcst.Substring(1);
                }
        
                // Validate the carrier and population - 99 or less
                if (carrier.Length > 2)
                {
                    this.BufferBuilder.AppendLine("Invalid carrier value!");
                    return;
                }
                else if (population.Length > 2)
                {
                    this.BufferBuilder.AppendLine("Invalid population value!");
                    return;
                }

                if (this.TryConnect())
                {
                    int commandResult = this.Login(false);
                    if (commandResult == 1)
                    {        
                        string command = "cd cmdutil/bin";
                        this.SendCommand(command);

                        this.Telnet.ClearBuffer();

                        command = "settermadmin " + this.ncc.IpAddress + " 0 " + rcst;
                        ExtendedLogger.Write(command, Category.General, Priority.Low);
                        this.SendCommand(command);
                        Thread.Sleep(3000);
                        
                        command = "setcarterm " + this.ncc.IpAddress + " 1 " + carrier + " " + rcst;
                        ExtendedLogger.Write(command, Category.General, Priority.Low);
                        this.SendCommand(command);
                        Thread.Sleep(3000);
                                      
                        string popLong = "011e00";
                        if (population.Length == 1)
                        {
                            popLong += "0";
                        }

                        popLong += population;
                        command = "moveterm 1e " + this.ncc.IpAddress + " " + popLong + " " + rcst;
                        ExtendedLogger.Write(command, Category.General, Priority.Low);
                        this.TryWriteAndWait(command, "?]");
         
                        // Reply with yes to send command to the rcst
                        command = "y\n";
                        this.TryWriteAndWait(command, ">");

                        // re-enable the terminal                
                        command = "settermadmin " + this.ncc.IpAddress + " 1 " + rcst;
                        ExtendedLogger.Write(command, Category.General, Priority.Low);
                        this.SendCommand(command);
                        Thread.Sleep(2000);

                        // send it again, we don't want to miss it                
                        this.SendCommand(command);

                        this.Telnet.ClearBuffer();
                    }
                    else
                    {
                        this.BufferBuilder.AppendLine();
                        this.BufferBuilder.AppendLine("Failed to successfully contact RNCC.");
                    }

                    this.Disconnect();
                    this.WriteCarrierMoveToDB(rcst, carrier, population, removeFromPenaltyBox, 12);
                }
            }
        }

        /// <summary>
        /// Cleans up resources used by this class.
        /// </summary>
        /// <param name="disposing">True if cleaning up managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                lock (this.LockObject)
                {
                    if (disposing)
                    {
                        if (this.ncc != null)
                        {
                            this.ncc.Dispose();
                        }
                    }

                    this.disposed = true;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Writes carrier move information to the DB.
        /// </summary>
        /// <param name="rcst">The RCST that was moved.</param>
        /// <param name="carrierId">The new carrier id.</param>
        /// <param name="populationId">The new population id.</param>
        /// <param name="removeFromPenaltyBox">A value indicating whether or not the RCST was removed from the penalty box.</param>
        /// <param name="operatorId">The id of the operator that made the move.</param>
        private void WriteCarrierMoveToDB(string rcst, string carrierId, string populationId, bool removeFromPenaltyBox, int operatorId) 
        {
            // strip off the 0x.
            if (rcst.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // Pad it out to 3 bytes (6 characters).
                rcst = rcst.Substring(2).PadLeft(6, '0');
            }

            string messageEnd = " RCST " + rcst + " to carrier " + carrierId + " population " + populationId;
            if (removeFromPenaltyBox)
            {
                ExtendedLogger.Write("Remove from PenaltyBox: " + rcst, Category.General, Priority.Low);
            }
    
            try
            {
                using (DbCommand cmd = this.Database.GetStoredProcCommand(
                    "reqRCSTCarrierMove",
                    rcst,
                    carrierId,
                    populationId,
                    removeFromPenaltyBox,
                    operatorId))
                {
                        cmd.ExecuteNonQuery();
                }

                this.BufferBuilder.AppendLine("Moved " + messageEnd); 
            }
            catch (SqlException ex)
            {
                string message = "Error moving " + messageEnd;
                ExtendedLogger.WriteException(message, Category.Config, Priority.High, ex);
                this.BufferBuilder.AppendLine(message);
            }
        }

        /// <summary>
        /// Checks that the NCC IP address is good.
        /// </summary>
        /// <returns>True if the NCC IP address is good.</returns>
        private bool CheckNccIPAddress()
        {
            // Check for valid ncc IP address. 7 characters is the min valid ip address.
            bool isGood = true;
            if (string.IsNullOrEmpty(this.ncc.IpAddress) || this.ncc.IpAddress.Length < 7)
            {
                string message = "NCC IpAddress is not valid. Currently set to: " + this.ncc.IpAddress;
                this.BufferBuilder.AppendLine(message);
                ExtendedLogger.Write(message, Category.Config, Priority.High);
                isGood = false;
            }

            return isGood;
        }
    }
}
