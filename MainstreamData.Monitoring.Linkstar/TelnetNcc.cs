// <copyright file="TelnetNcc.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.Linkstar
{
    using System.Data.SqlClient;
    using System.Web;

    /// <summary>
    /// Provides an interface into the NCC.
    /// </summary>
    public class TelnetNcc : TelnetLinkstar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TelnetNcc"/> class.
        /// </summary>
        /// <param name="connectionStringBuilder">The Linkstar DB connection info.</param>
        /// <param name="region">The RNCC region value.</param>
        public TelnetNcc(SqlConnectionStringBuilder connectionStringBuilder, string region)
            : base(connectionStringBuilder, region, LinkstarHostType.Ncc)
        {
        }

        /// <summary>
        /// Performs the specified action on the specified rcst.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        /// <param name="rcst">The rcst to perform the action on.</param>
        public override void Execute(string action, string rcst)
        {
            lock (this.LockObject)
            {
                TelnetNcc.FixTermId(ref rcst);
                if (this.TryConnect())
                {
                    // Executing rttelnet on ncc results in "illegal command", so setting it to false.
                    this.LoginAndSendCommand("cd cmdutil/bin", false);

                    action = action.ToUpperInvariant();
                    string commmandStart = "settermadmin " + this.IpAddress;
                    int commandNumber = -1;
                    bool wait = true;
                    switch (action)
                    {
                        case "DT":
                            // Disable terminal.
                            commandNumber = 0;
                            break;
                        case "ET":
                            // Enable terminal Two-way.
                            commandNumber = 1;
                            break;
                        case "RTO":
                            // Enable terminal RX (TX Optional).
                            commandNumber = 2;
                            ////wait = false;
                            break;
                        case "RO":
                            // Enable terminal RX Only.
                            commandNumber = 3;
                            break;
                        default:
                            this.BufferBuilder.AppendLine(action + " is not a valid action.");
                            break;
                    }

                    if (commandNumber != -1)
                    {
                        const string WaitFor = "Admin Mode set to";
                        string command = commmandStart + " " + commandNumber + " " + rcst;

                        if (wait)
                        {
                            this.TryWriteAndWait(command, WaitFor);
                        }
                        else
                        {
                            // This doesn't seem to be working - may disconnect before command is actually sent to RCST, but TryWriteAndWait isn't much slower and is much more relaible anyway.
                            this.Telnet.WriteLine(command);
                            this.BufferBuilder.Append(HttpUtility.HtmlEncode(this.Telnet.Read()));  // This is typically very fast.
                        }
                    }

                    this.Disconnect();
                }
            }
        }
    }
}
