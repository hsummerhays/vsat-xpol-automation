// <copyright file="TelnetClient.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

// Code loosely based on minimalistic telnet implementation
// conceived by Tom Janssens on 2007/06/06  for codeproject
// http://www.corebvba.be
// see CodeProjectLicenseForTelnetClient.htm file in this project for license details.

namespace MainstreamData.Web
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// Verbs used while communicating with server.
    /// </summary>
    internal enum Verbs
    {
        /// <summary>
        /// Value for will.
        /// </summary>
        WILL = 251,

        /// <summary>
        /// Value for won't.
        /// </summary>
        WONT = 252,

        /// <summary>
        /// Value for do.
        /// </summary>
        DO = 253,

        /// <summary>
        /// Value for don't
        /// </summary>
        DONT = 254,

        /// <summary>
        /// Value for IAC.
        /// </summary>
        IAC = 255
    }

    /// <summary>
    /// Options to use while communicating with server.
    /// </summary>
    internal enum Options
    {
        /// <summary>
        /// Value for SGA.
        /// </summary>
        SGA = 3
    }

    /// <summary>
    /// Allows communication with Telnet servers.
    /// </summary>
    public class TelnetClient : IDisposable
    {
        /// <summary>
        /// Used to connect to the telnet server via tcp.
        /// </summary>
        private TcpClient tcpSocket;

        /// <summary>
        /// Tracks whether or not the class has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Holds all of the read results until cleared.
        /// </summary>
        private StringBuilder buffer = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="TelnetClient"/> class.
        /// </summary>
        /// <param name="hostName">The host name or ip address of the telnet server.</param>
        /// <param name="port">The IP port to connect on.</param>
        public TelnetClient(string hostName, int port)
        {
            this.tcpSocket = new TcpClient(hostName, port);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TelnetClient"/> class. Exists to avoid cleaning up items too soon.
        /// </summary>
        ~TelnetClient()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the current contents of the buffer.
        /// </summary>
        public string Buffer
        {
            get
            {
                return this.buffer.ToString();
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the client is connected to the server.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.tcpSocket.Connected;
            }
        }

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void ClearBuffer()
        {
            this.buffer = new StringBuilder();
        }

        /// <summary>
        /// Logs into the telnet server using a default timeout of 1000 ms between prompts.
        /// </summary>
        /// <param name="username">The username to use.</param>
        /// <param name="password">The password to use.</param>
        /// <returns>The data returned by the telnet server as part of the login process.</returns>
        public string Login(string username, string password)
        {
            return this.Login(username, password, 1000);
        }

        /// <summary>
        /// Logs into the telnet server.
        /// </summary>
        /// <param name="username">The username to use.</param>
        /// <param name="password">The password to use.</param>
        /// <param name="timeout">The time to wait in ms for a response between prompts.</param>
        /// <returns>The data returned by the telnet server as part of the login process.</returns>
        /// <exception cref="WebException">Thrown if don't get login prompt or password prompt.</exception>
        public string Login(string username, string password, int timeout)
        {
            string s = this.Read(timeout);
            if (!s.TrimEnd().EndsWith(":"))
            {
                throw new WebException("Failed to connect : no login prompt");
            }

            this.WriteLine(username);

            s += this.Read(timeout);
            if (!s.TrimEnd().EndsWith(":"))
            {
                throw new WebException("Failed to connect : no password prompt");
            }

            this.WriteLine(password);

            s += this.Read(timeout);
            return s;
        }

        /// <summary>
        /// Writes the specified command to the telnet server followed by a linefeed character.
        /// </summary>
        /// <param name="cmd">The command to write.</param>
        public void WriteLine(string cmd)
        {
            this.Write(cmd + "\n");
        }

        /// <summary>
        /// Writes the specified command to the telnet server.
        /// </summary>
        /// <param name="cmd">The command to write.</param>
        public void Write(string cmd)
        {
            if (!this.tcpSocket.Connected)
            {
                return;
            }

            byte[] buf = ASCIIEncoding.ASCII.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
            this.tcpSocket.GetStream().Write(buf, 0, buf.Length);
        }

        /// <summary>
        /// Writes the command to the server then waits for the specified string. Uses a default timeout of 10 seconds.
        /// </summary>
        /// <param name="command">The command to write.</param>
        /// <param name="waitFor">The string to wait for.</param>
        /// <returns>The data returned by the telnet server.</returns>
        /// <exception cref="TimeoutException">Thrown if the string to wait for is not found within the specified timeout.</exception>
        public string WriteAndWait(string command, string waitFor)
        {
            return this.WriteAndWait(command, waitFor, 10);
        }

        /// <summary>
        /// Writes the command to the server then waits for the specified string.
        /// </summary>
        /// <param name="command">The command to write.</param>
        /// <param name="waitFor">The string to wait for.</param>
        /// <param name="timeoutSeconds">The time to wait for the string.</param>
        /// <returns>The data returned by the telnet server.</returns>
        /// <exception cref="TimeoutException">Thrown if the string to wait for is not found within the specified timeout.</exception>
        public string WriteAndWait(string command, string waitFor, int timeoutSeconds)
        {
            return this.WriteAndWait(command, new string[] { waitFor }, timeoutSeconds);
        }

        /// <summary>
        /// Writes the command to the server then waits for the specified string. Uses a default timeout of 10 seconds.
        /// </summary>
        /// <param name="command">The command to write.</param>
        /// <param name="waitFor">The strings to look for. If any of the strings are found, the method exits.</param>
        /// <returns>The data returned by the telnet server.</returns>
        /// <exception cref="TimeoutException">Thrown if the string to wait for is not found within the specified timeout.</exception>
        public string WriteAndWait(string command, string[] waitFor)
        {
            return this.WriteAndWait(command, waitFor, 10);
        }

        /// <summary>
        /// Writes the command to the server then waits for the specified string.
        /// </summary>
        /// <param name="command">The command to write.</param>
        /// <param name="waitFor">The strings to look for. If any of the strings are found, the method exits.</param>
        /// <param name="timeoutSeconds">The time to wait for the string.</param>
        /// <returns>The data returned by the telnet server.</returns>
        /// <exception cref="TimeoutException">Thrown if the string to wait for is not found within the specified timeout.</exception>
        public string WriteAndWait(string command, string[] waitFor, int timeoutSeconds)
        {
            this.WriteLine(command);
            return this.Wait(waitFor, timeoutSeconds);
        }

        /// <summary>
        /// Waits for the specified string to return from the server. Uses a default timeout of 10 seconds.
        /// </summary>
        /// <param name="waitFor">The string to wait for.</param>
        /// <returns>The data returned by the telnet server.</returns>
        /// <exception cref="TimeoutException">Thrown if the string to wait for is not found within the specified timeout.</exception>
        public string Wait(string waitFor)
        {
            return this.Wait(new string[] { waitFor }, 10);
        }

        /// <summary>
        /// Waits for the specified string to return from the server.
        /// </summary>
        /// <param name="waitFor">The string to wait for.</param>
        /// <param name="timeoutSeconds">The time to wait for the string.</param>
        /// <returns>The data returned by the telnet server.</returns>
        /// <exception cref="TimeoutException">Thrown if the string to wait for is not found within the specified timeout.</exception>        
        public string Wait(string waitFor, int timeoutSeconds)
        {
            return this.Wait(new string[] { waitFor }, timeoutSeconds);
        }

        /// <summary>
        /// Waits for the specified string to return from the server. Uses a default timeout of 10 seconds.
        /// </summary>
        /// <param name="waitFor">The strings to look for. If any of the strings are found, the method exits.</param>
        /// <returns>The data returned by the telnet server.</returns>
        /// <exception cref="TimeoutException">Thrown if the string to wait for is not found within the specified timeout.</exception>        
        public string Wait(string[] waitFor)
        {
            return this.Wait(waitFor, 10);
        }

        /// <summary>
        /// Waits for the specified string to return from the server.
        /// </summary>
        /// <param name="waitFor">The strings to look for. If any of the strings are found, the method exits.</param>
        /// <param name="timeoutSeconds">The time to wait for the string.</param>
        /// <returns>The data returned by the telnet server.</returns>
        /// <exception cref="TimeoutException">Thrown if the string to wait for is not found within the specified timeout.</exception>        
        public string Wait(string[] waitFor, int timeoutSeconds)
        {
            StringBuilder localBuffer = new StringBuilder();
            DateTime start = DateTime.UtcNow;
            bool stringFound = false;
            do
            {
                localBuffer.Append(this.Read());
                if (localBuffer.Length >= waitFor.Length)
                {
                    foreach (string waitForValue in waitFor)
                    {
                        stringFound = localBuffer.ToString().Contains(waitForValue);
                        if (stringFound)
                        {
                            break;
                        }
                    }
                }
            }
            while (start.AddSeconds(timeoutSeconds) > DateTime.UtcNow && !stringFound);

            if (!stringFound)
            {
                throw new TimeoutException("String '" + string.Join(", ", waitFor) + "' was not found in the results within the timeout period of " + timeoutSeconds + " seconds.");
            }

            return localBuffer.ToString();
        }

        /// <summary>
        /// Reads data back from the telnet server using a default wait interval of 100 ms.
        /// </summary>
        /// <returns>The data returned by the telnet server</returns>
        public string Read()
        {
            return this.Read(100);
        }

        /// <summary>
        /// Reads data back from the telnet server.
        /// </summary>
        /// <param name="wait">The wait interval in miliseconds to use.</param>
        /// <returns>The data returned by the telnet server.</returns>
        public string Read(int wait)
        {
            if (!this.tcpSocket.Connected)
            {
                return null;
            }

            StringBuilder localBuffer = new StringBuilder();
            do
            {
                System.Threading.Thread.Sleep(wait);
                this.ParseTelnet(localBuffer);
            } 
            while (this.tcpSocket.Available > 0);

            string result = localBuffer.ToString();
            this.buffer.Append(result);
            return result;
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
        /// Cleans up resources used by this class.
        /// </summary>
        /// <param name="disposing">True if cleaning up managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.tcpSocket.Close();
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Parses data read from the telnet server.
        /// </summary>
        /// <param name="localBuffer">The string builder object that contains the data.</param>
        private void ParseTelnet(StringBuilder localBuffer)
        {
            while (this.tcpSocket.Available > 0)
            {
                int input = this.tcpSocket.GetStream().ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        int inputverb = this.tcpSocket.GetStream().ReadByte();
                        if (inputverb == -1)
                        {
                            break;
                        }

                        switch (inputverb)
                        {
                            case (int)Verbs.IAC: 
                                // literal IAC = 255 escaped, so append char 255 to string
                                localBuffer.Append(inputverb);
                                break;
                            case (int)Verbs.DO: 
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppress go ahead)
                                int inputoption = this.tcpSocket.GetStream().ReadByte();
                                if (inputoption == -1)
                                {
                                    break;
                                }

                                this.tcpSocket.GetStream().WriteByte((byte)Verbs.IAC);
                                if (inputoption == (int)Options.SGA)
                                {
                                    this.tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                }
                                else
                                {
                                    this.tcpSocket.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                }

                                this.tcpSocket.GetStream().WriteByte((byte)inputoption);
                                break;
                            default:
                                break;
                        }

                        break;
                    default:
                        localBuffer.Append((char)input);
                        break;
                }
            }
        }
    }
}