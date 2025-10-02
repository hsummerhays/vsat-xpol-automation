// <copyright file="MpForm.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring
{
    using System;
    using System.ComponentModel;
    using System.Windows.Forms;
    using MainstreamData.Logging;
    using MainstreamData.Utility;

    /// <summary>
    /// The only UI form shown when running in application mode.
    /// </summary>
    public partial class MPForm : Form
    {
        /// <summary>
        /// A value indicating whether the form is closing.
        /// </summary>
        private bool closing = false;

        /// <summary>
        /// Create a MonitorPoint object to do all the work.
        /// </summary>
        private MonitorPoint monitorPoint = null;

        /// <summary>
        /// Initializes a new instance of the MPForm class.
        /// </summary>
        public MPForm()
        {
            this.InitializeComponent();

            // Call Message method when monitorPoint.Message event fires
            ExtendedLogger.MessageLogged += new EventHandler<LogEventArgs>(this.Message);
            ExtendedLogger.IgnoreDebugForMessageLoggedEvent = true;
        }

        /// <summary>
        /// Initializes a new instance of the MPForm class.
        /// </summary>
        /// <param name="title">The tile to use for the form.</param>
        /// <param name="monitorPoint">The monitor point class to use with the form.</param>
        public MPForm(string title, MonitorPoint monitorPoint)
            : this()
        {
            this.monitorPoint = monitorPoint;
            this.SetTitle(title, title + " for HAL.  Please do not close.");
        }

        /// <summary>
        /// Allows for asynchronous calls for setting uiMessage properties.
        /// </summary>
        /// <param name="sender">Object that called the method.</param>
        /// <param name="e">Arguments passed to the method.</param>
        private delegate void SetMessageCallback(object sender, LogEventArgs e);

        /// <summary>
        /// Changes the title to include the specified text instead of the default "Reuters WNE"
        /// </summary>
        /// <param name="formTitle">Title to display on the form.</param>
        /// <param name="formDescription">Description to display in the form.</param>
        public void SetTitle(string formTitle, string formDescription)
        {
            this.Text = formTitle;
            this.uiTitle.Text = formDescription;
        }

        /// <summary>
        /// When running as an application, this method allows some initial setup.
        /// </summary>
        /// <param name="sender">See .Net documentation for more info.</param>
        /// <param name="e">Allows access to command line arguments.</param>
        private void FormMain_Load(object sender, EventArgs e)
        {
            // Populate the gridview
            this.uiMessage.Columns.Add("Time", "Time");
            this.uiMessage.Columns.Add("Type", "Type");
            this.uiMessage.Columns.Add("Detail", "Detail");
            this.uiMessage.Columns[0].Width = 125;
            this.uiMessage.Columns[1].Width = 65;
            this.uiMessage.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            // Show the version
            this.uiVersion.Text += ApplicationInfo.Version;

            // Start the timers
            if (this.monitorPoint != null)
            {
                this.monitorPoint.Start(this);
            }
        }

        /// <summary>
        /// Event wired method that writes a message to the screen.
        /// </summary>
        /// <param name="sender">Object that called the method.</param>
        /// <param name="e">Arguments passed to the method.</param>
        private void Message(object sender, LogEventArgs e)
        {
            if (!this.closing)
            {
                // Invoke anonymous method so code will always execute on UI thread and allow other threads to continue.
                this.BeginInvoke((MethodInvoker)delegate
                {
                    // When form is closing this method may still get called - check column count to avoid error.
                    if (this.uiMessage.ColumnCount > 0)
                    {
                        // Write events to screen
                        this.uiMessage.Rows.Insert(0, DateTime.Now, e.Category, e.Message);
                        this.uiMessage.Rows[0].Cells[2].ToolTipText = e.Message;

                        // Move to the current message
                        this.uiMessage.CurrentCell = this.uiMessage.Rows[0].Cells[0];

                        // Delete old messages if needed
                        if (this.uiMessage.RowCount > 1000)
                        {
                            this.uiMessage.Rows.RemoveAt(this.uiMessage.RowCount - 1);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Executes when form is closing.
        /// </summary>
        /// <param name="sender">See .Net documentation for info about sender.</param>
        /// <param name="e">See .Net documentation for info about FormClosingEventArgs</param>
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.closing = true;
            this.monitorPoint.Stop();
        }
    }
}