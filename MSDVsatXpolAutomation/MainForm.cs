// <copyright file="MainForm.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.Xpol
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.ServiceModel;
    using System.Windows.Forms;
    using System.Windows.Forms.DataVisualization.Charting;
    using MainstreamData.Monitoring;
    using MainstreamData.Utility;

    /// <summary>
    /// Main form for interfacing the application.
    /// </summary>
    public class MainForm : Form, IDisposable
    {
        /// <summary>
        ///  The name of the satellite we are connecting to.
        /// </summary>
        private const string SatelliteName = "AMC16";

        /// <summary>
        /// One of many UI labels.
        /// </summary>
        private Label label1;

        /// <summary>
        /// One of many UI labels.
        /// </summary>
        private Label label2;

        /// <summary>
        /// One of many UI labels.
        /// </summary>
        private Label label3;

        /// <summary>
        /// One of many UI labels.
        /// </summary>
        private Label label4;

        /// <summary>
        /// One of many UI labels.
        /// </summary>
        private Label label5;

        /// <summary>
        /// One of many UI labels.
        /// </summary>
        private Label label6;

        /// <summary>
        /// A UI label to show status information.
        /// </summary>
        private Label uiStatus;

        /// <summary>
        /// Button to start the scanning process.
        /// </summary>
        private Button uiStartButton;

        /// <summary>
        /// Button to stop the scanning process.
        /// </summary>
        private Button uiStopButton;

        /// <summary>
        /// Textbox for showing Copol value.
        /// </summary>
        private TextBox uiCopolAmplitude;

        /// <summary>
        /// Textbox for showing Crosspol value.
        /// </summary>
        private TextBox uiXpolAmplitude;

        /// <summary>
        /// Textbot for showing isolation value.
        /// </summary>
        private TextBox uiIsolation;

        /// <summary>
        /// Textbox for showing selected clear wave frequency.
        /// </summary>
        private TextBox uiCurrentCopolFrequency;

        /// <summary>
        /// Chart for showing copol spectrum analyzer info.
        /// </summary>
        private Chart uiCopolChart;

        /// <summary>
        /// Chart for showing crosspol spectrum anaylzer info.
        /// </summary>
        private Chart uiXpolChart;

        /// <summary>
        /// Combobox for selecting the clear wave frequency.
        /// </summary>
        private ComboBox uiFrequency;

        /// <summary>
        /// Instance of IsolationAnalyzer class for interfacing spec anaylers.
        /// </summary>
        ////private IsolationAnalyzer isoAnalyzer = new IsolationAnalyzer();

        /// <summary>
        /// Timer to prevent clear wave signal states from changing to inactive.
        /// </summary>
        private Timer timer;

        /// <summary>
        /// Necessary for timer disposal.
        /// </summary>
        private IContainer components;

        /// <summary>
        /// Track whether or not an instance of this class has been disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Client for making calls to Windows Service that controls the spectrum analyzers.
        /// </summary>
        ////private VsatXpolRmpHost.VsatXpolRmpClient rmpClient = new VsatXpolRmpHost.VsatXpolRmpClient();

        /// <summary>
        /// Initializes a new instance of the MainForm class.
        /// </summary>
        public MainForm()
        {
            // Required for Windows Form Designer support
            this.InitializeComponent();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.Run(new MainForm());
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed.</param>
        protected override void Dispose(bool disposing)
        {
            // Check disposed field so Dispose does not get done more than once
            try
            {
                if (!this.disposed)
                {
                    if (disposing)
                    {
                        /*if (this.isoAnalyzer != null)
                        {
                            this.isoAnalyzer.Dispose();
                        }*/

                        /*if (!this.rmpClient.State.In(CommunicationState.Faulted, CommunicationState.Closed, CommunicationState.Closing))
                        {
                            try
                            {
                                this.rmpClient.Close();
                            }
                            finally
                            {
                                // TODO: Consider catching EndpointNotFoundException.
                            }
                        }*/
                        
                        // TODO: Add code to dispose of LMP's attached to VsatXpolLmpList.
                    }
                }

                this.disposed = true;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Update UI with current status.
        /// </summary>
        /// <param name="status">New status text to display.</param>
        private void UpdateStatus(string status)
        {
            this.uiStatus.Text = status;
            this.uiStatus.Refresh();
        }

        /// <summary>
        /// Fires when the form is loaded. Used to setup UI components.
        /// </summary>
        /// <param name="sender">The form instance object.</param>
        /// <param name="e">Arguments that apply to this event.</param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            Properties.Settings settings = Properties.Settings.Default;

            // Update CW frequency lists
            this.uiFrequency.Items.Add(settings.CW1LbandFreqMhz);
            this.uiFrequency.Items.Add(settings.CW2LbandFreqMhz);
            this.uiFrequency.Items.Add(settings.CW3LbandFreqMhz);
            this.uiFrequency.Items.Add(settings.CW4LbandFreqMhz);
            this.uiFrequency.Items.Add(settings.CW5LbandFreqMhz);
            this.uiFrequency.SelectedIndex = 4; // TODO: Set this back to zero.

            // Setup isoAnalyzer Object
            /*this.isoAnalyzer.Error += new EventHandler<AnalyzerEventArgs>(this.IsoAnalyzer_Error);
            this.isoAnalyzer.FreshCopolData += new EventHandler<AnalyzerEventArgs>(this.IsoAnalyzer_FreshCopolData);
            this.isoAnalyzer.FreshXpolData += new EventHandler<AnalyzerEventArgs>(this.IsoAnalyzer_FreshXpolData);
            this.isoAnalyzer.BeaconsFound += new EventHandler<AnalyzerEventArgs>(this.IsoAnalyzer_BeaconsFound);

            this.UpdateStatus("Checking Copol Analzyer");
            this.Show();
            this.Refresh();
            this.isoAnalyzer.CopolAnalyzerGpibBoardNumber = settings.SpectrumAnalyzerGpibBoardNumCopol;
            this.isoAnalyzer.CopolAnalyzerAddress = settings.SpectrumAnalyzerAddressCopol;

            this.UpdateStatus("Checking Xpol Analyzer");
            this.Refresh();
            this.isoAnalyzer.XpolAnalyzerGpibBoardNumber = settings.SpectrumAnalyzerGpibBoardNumXpol;
            this.isoAnalyzer.XpolAnalyzerAddress = settings.SpectrumAnalyzerAddressXpol;*/
            this.Show();
            this.UpdateStatus("Opening connection to RMP");
            this.Refresh();

            if (this.InitializeLmp())
            {
                try
                {
                    ////this.rmpClient.Open();
                    ////this.rmpClient.Initialize(signalPairList.ToArray());
                    this.UpdateStatus("Waiting for User Input");
                }
                catch (InvalidOperationException ex)
                {
                    this.UpdateStatus(ex.Message);
                }
            }

            /*catch (TimeoutException)
            {
                this.UpdateStatus("Timeout while opening connection to RMP");
                this.uiStartButton.Enabled = false;
            }
            catch (CommunicationObjectFaultedException)
            {
                this.UpdateStatus("RMP is in a faulted state. Please review the log files on the RMN and correct the problem.");
                this.uiStartButton.Enabled = false;
            }
            catch (EndpointNotFoundException)
            {
                this.UpdateStatus("Endpoint not found when opening connection to RMP");
            }*/

            ////this.UpdateStatus("Waiting for User Input");
        }

        /// <summary>
        /// Initializes the LMP (i.e. connects to the RMP and calls initialize method).
        /// </summary>
        /// <returns>True if successful.</returns>
        private bool InitializeLmp()
        {
            bool success = false;
            try
            {
                if (VsatXpolLmpList.Dictionary.Count == 0)
                {
                    VsatXpolLmpList.Add(MainForm.SatelliteName, "VSAT1, VSAT2", "http://vsat2rmn:8000/ServiceModel/vsatxpolrmp");
                }

                Properties.Settings settings = Properties.Settings.Default;
                List<SignalPair> signalPairList = new List<SignalPair>();
                signalPairList.Add(new SignalPair(
                    SpecAnalyzer.ConvertToHertz(settings.BeaconCopolLbandFreqMhz.ToString()),
                    SpecAnalyzer.ConvertToHertz(settings.BeaconCrosspolLbandFreqMhz.ToString())));
                signalPairList.Add(new SignalPair(
                    SpecAnalyzer.ConvertToHertz(settings.CW1LbandFreqMhz.ToString())));
                signalPairList.Add(new SignalPair(
                    SpecAnalyzer.ConvertToHertz(settings.CW2LbandFreqMhz.ToString())));
                signalPairList.Add(new SignalPair(
                    SpecAnalyzer.ConvertToHertz(settings.CW3LbandFreqMhz.ToString())));
                signalPairList.Add(new SignalPair(
                    SpecAnalyzer.ConvertToHertz(settings.CW4LbandFreqMhz.ToString())));
                signalPairList.Add(new SignalPair(
                    SpecAnalyzer.ConvertToHertz(settings.CW5LbandFreqMhz.ToString())));

                VsatXpolLmpList.GetLmp(MainForm.SatelliteName).Initialize(signalPairList);
                success = true;
            }
            catch (InvalidOperationException ex)
            {
                this.UpdateStatus(ex.Message);
            }

            return success;
        }

        /// <summary>
        /// Cleans up application before exiting.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The even arguments.</param>
        private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.uiStopButton.PerformClick();
        }

        /// <summary>
        /// Stops the application from interfacing the analyzers when frequency changes.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The even arguments.</param>
        private void SelectedFrequency_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            this.uiStopButton.PerformClick();
        }

        /// <summary>
        /// Causes the application to start interfacing the analyzers and displaying data.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The even arguments.</param>
        private void StartButton_Click(object sender, System.EventArgs e)
        {
            if (!this.InitializeLmp())
            {
                return;
            }

            // Disable start button.
            this.uiStartButton.Enabled = false;

            /*string[] messages = this.rmpClient.get_RecentErrorMessageList();
            int length = messages.Length;
            if (length > 0)
            {
                string message = messages[length - 1];
                if (message.Substring(0, 4).ToLower() == "high")
                {
                    MessageBox.Show("Recent error message: " + messages[length - 1]);
                }
            }*/
            string message = null;
            try
            {
                message = VsatXpolLmpList.GetLmp(MainForm.SatelliteName).GetLastErrorMessage();
            }
            catch (InvalidOperationException ex)
            {
                message = ex.Message;
            }

            if (!string.IsNullOrEmpty(message))
            {
                this.UpdateStatus(message);
                MessageBox.Show("Recent error message: " + message);
            }

            // Use the current cw frequency. Must set isoAnalyzer.ClearWaveList property for proper update.
            /*int frequency = (int)(double.Parse(this.uiFrequency.Text) * 1E6);
            SignalPair[] list = this.isoAnalyzer.ClearWaveList;
            list[0] = new SignalPair(frequency);
            this.isoAnalyzer.ClearWaveList = list;*/

            // Clear old values.
            this.uiCopolAmplitude.Text = string.Empty;
            this.uiXpolAmplitude.Text = string.Empty;
            this.uiIsolation.Text = string.Empty;
            this.uiCurrentCopolFrequency.Text = string.Empty;

            // Start search for beacons.
            /*this.isoAnalyzer.CopolBeacon.Frequency =
                (int)(Properties.Settings.Default.BeaconCopolLbandFreqMhz * 1E6);
            this.isoAnalyzer.XpolBeacon.Frequency =
                (int)(Properties.Settings.Default.BeaconCrosspolLbandFreqMhz * 1E6);
            this.isoAnalyzer.Reset();

            this.UpdateStatus("Waiting for beacons to be found.");*/

            if (!this.uiStartButton.Enabled)
            {
                // No errors encountered - enable stop button.
                this.uiStopButton.Enabled = true;
                this.timer.Enabled = true;
                this.Timer_Tick(this, new EventArgs());
            }
        }

        /// <summary>
        /// Stops the application from interfacing the analyzers.
        /// </summary>
        /// <param name="sender">The object that called the method.</param>
        /// <param name="e">The arguments for the method.</param>
        private void StopButton_Click(object sender, System.EventArgs e)
        {
            this.uiStopButton.Enabled = false;
            this.timer.Enabled = false;
            this.UpdateStatus("Waiting for User Input.");
            ////this.isoAnalyzer.Stop();
            this.uiStartButton.Enabled = true;
        }

        /// <summary>
        /// Shows user any errors that the IsoAnalyzer class may have encountered.
        /// </summary>
        /// <param name="sender">The object that called the method.</param>
        /// <param name="e">The arguments for the method.</param>
        private void IsoAnalyzer_Error(object sender, AnalyzerEventArgs e)
        {
            // Invoke anonymous method so code will always execute on UI thread and allow other threads to continue.
            this.BeginInvoke((MethodInvoker)delegate
            {
                this.uiStopButton.PerformClick();

                MessageBox.Show(e.Message);
            });
        }

        /// <summary>
        /// Starts sweeping for CW data once both beacons are found.
        /// </summary>
        /// <param name="sender">The object that called the method.</param>
        /// <param name="e">The arguments for the method.</param>
        private void IsoAnalyzer_BeaconsFound(object sender, EventArgs e)
        {
            // Invoke anonymous method so code will always execute on UI thread and allow other threads to continue.
            this.BeginInvoke((MethodInvoker)delegate
            {
                if (!this.uiStartButton.Enabled)
                {
                    this.UpdateStatus("Beacons were found.");
                }
            });
        }

        /// <summary>
        /// Fills the specified chart with data.
        /// </summary>
        /// <param name="chart">The chart to fill with data.</param>
        /// <param name="values">The values to fill the chart with.</param>
        private void FillChart(Chart chart, List<float> values)
        {
            // Invoke anonymous method so code will always execute on UI thread and allow other threads to continue.
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Add new data to chart.
                DataPointCollection dataPoints = chart.Series[0].Points;
                dataPoints.Clear();
                dataPoints.DataBindY(values);
            });
        }

        /// <summary>
        /// Updates amplitude and xpol values.
        /// </summary>
        private void UpdateValues()
        {
            // Invoke anonymous method so code will always execute on UI thread and allow other threads to continue.
            this.BeginInvoke((MethodInvoker)delegate
            {
                // Moved code below to Timer_Tick to simplify changes.
                /*this.uiCopolAmplitude.Text = Math.Round(this.isoAnalyzer.ClearWaveList[0].CopolSignal.Amplitude, 2).ToString();
                this.uiXpolAmplitude.Text = Math.Round(this.isoAnalyzer.ClearWaveList[0].XpolSignal.Amplitude, 2).ToString();
                this.uiIsolation.Text = Math.Round(this.isoAnalyzer.ClearWaveList[0].Isolation, 2).ToString();
                this.uiCurrentCopolFrequency.Text = Math.Round((double)this.isoAnalyzer.ClearWaveList[0].CopolSignal.ActualFrequency / 1000000, 6).ToString();*/

                if (!this.uiStartButton.Enabled)
                {
                    this.UpdateStatus("Waiting for fresh data.");
                }
            });
        }

        /// <summary>
        /// Fires when there is fresh data for the copol signal.
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="e">The arguments for the event.</param>
        private void IsoAnalyzer_FreshCopolData(object sender, AnalyzerEventArgs e)
        {
            this.FillChart(this.uiCopolChart, (List<float>)e.Signal.Data);
            if (e.Signal.SignalType != SignalType.Beacon)
            {
                this.UpdateValues();
            }
        }

        /// <summary>
        /// Fires when there is fresh data for the xpol signal.
        /// </summary>
        /// <param name="sender">The object that fired the event.</param>
        /// <param name="e">The arguments for the event.</param>
        private void IsoAnalyzer_FreshXpolData(object sender, AnalyzerEventArgs e)
        {
            this.FillChart(this.uiXpolChart, (List<float>)e.Signal.Data);
            if (e.Signal.SignalType != SignalType.Beacon)
            {
                this.UpdateValues();
            }
        }

        /// <summary>
        /// Update retrieval time on all clear waves so they remain active.
        /// </summary>
        /// <param name="sender">Object that called the method.</param>
        /// <param name="e">Arguments for the event.</param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            /*SignalPair[] clearWaveList = this.isoAnalyzer.ClearWaveList;
            int listLength = clearWaveList.GetUpperBound(0) + 1;
            for (int i = 0; i < listLength; i++)
            {
                clearWaveList[i].UpdateRetrievalTime();
            }*/

            SignalPair pair = null;
            try
            {
                ////SignalPair beacons = this.rmpClient.GetSignalPair(0);
                try
                {
                    VsatXpolLmp lmp = VsatXpolLmpList.GetLmp(MainForm.SatelliteName);
                    SignalPair beacons = lmp.GetSignalPair(0);
                    if (beacons.CopolSignal.State != SignalState.Found || beacons.XpolSignal.State != SignalState.Found)
                    {
                        pair = beacons;
                        this.UpdateStatus("Beacon(s) not yet found.");
                    }
                    else
                    {
                        pair = lmp.GetSignalPair(this.uiFrequency.SelectedIndex + 1);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    this.UpdateStatus(ex.Message);
                }
            }
            catch (TimeoutException)
            {
                this.UpdateStatus("Timeout while getting status from RMP");
                return;
            }
            catch (CommunicationException ex)
            {
                this.UpdateStatus("CommunicationException while getting status from RMP. " + ex.Message);
                return;
            }

            if (pair == null)
            {
                return;
            }

            this.uiCopolAmplitude.Text = Math.Round(pair.CopolSignal.Amplitude, 2).ToString();
            this.uiXpolAmplitude.Text = Math.Round(pair.XpolSignal.Amplitude, 2).ToString();
            this.uiIsolation.Text = Math.Round(pair.Isolation, 2).ToString();
            this.uiCurrentCopolFrequency.Text = Math.Round((double)pair.CopolSignal.ActualFrequency / 1000000, 6).ToString();
            this.IsoAnalyzer_FreshCopolData(this, new AnalyzerEventArgs(pair.CopolSignal));
            this.IsoAnalyzer_FreshXpolData(this, new AnalyzerEventArgs(pair.XpolSignal));
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea5 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series5 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title5 = new System.Windows.Forms.DataVisualization.Charting.Title();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea6 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series6 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Title title6 = new System.Windows.Forms.DataVisualization.Charting.Title();
            this.uiCopolAmplitude = new System.Windows.Forms.TextBox();
            this.uiXpolAmplitude = new System.Windows.Forms.TextBox();
            this.uiIsolation = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.uiFrequency = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.uiStartButton = new System.Windows.Forms.Button();
            this.uiStopButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.uiStatus = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.uiCurrentCopolFrequency = new System.Windows.Forms.TextBox();
            this.uiCopolChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.uiXpolChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.timer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.uiCopolChart)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.uiXpolChart)).BeginInit();
            this.SuspendLayout();
            // 
            // uiCopolAmplitude
            // 
            this.uiCopolAmplitude.Location = new System.Drawing.Point(128, 70);
            this.uiCopolAmplitude.Name = "uiCopolAmplitude";
            this.uiCopolAmplitude.ReadOnly = true;
            this.uiCopolAmplitude.Size = new System.Drawing.Size(100, 20);
            this.uiCopolAmplitude.TabIndex = 2;
            // 
            // uiXpolAmplitude
            // 
            this.uiXpolAmplitude.Location = new System.Drawing.Point(128, 96);
            this.uiXpolAmplitude.Name = "uiXpolAmplitude";
            this.uiXpolAmplitude.ReadOnly = true;
            this.uiXpolAmplitude.Size = new System.Drawing.Size(100, 20);
            this.uiXpolAmplitude.TabIndex = 3;
            // 
            // uiIsolation
            // 
            this.uiIsolation.Location = new System.Drawing.Point(128, 120);
            this.uiIsolation.Name = "uiIsolation";
            this.uiIsolation.ReadOnly = true;
            this.uiIsolation.Size = new System.Drawing.Size(100, 20);
            this.uiIsolation.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(32, 73);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(88, 16);
            this.label1.TabIndex = 5;
            this.label1.Text = "Copol Amplitude";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(16, 99);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 16);
            this.label2.TabIndex = 6;
            this.label2.Text = "Crosspol Amplitude";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(72, 123);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(48, 16);
            this.label3.TabIndex = 7;
            this.label3.Text = "Isolation";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // uiFrequency
            // 
            this.uiFrequency.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.uiFrequency.Location = new System.Drawing.Point(128, 8);
            this.uiFrequency.Name = "uiFrequency";
            this.uiFrequency.Size = new System.Drawing.Size(121, 21);
            this.uiFrequency.TabIndex = 1;
            this.uiFrequency.SelectedIndexChanged += new System.EventHandler(this.SelectedFrequency_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(12, 11);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 18);
            this.label4.TabIndex = 9;
            this.label4.Text = "CW Frequency Slot";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // uiStartButton
            // 
            this.uiStartButton.Location = new System.Drawing.Point(419, 147);
            this.uiStartButton.Name = "uiStartButton";
            this.uiStartButton.Size = new System.Drawing.Size(75, 23);
            this.uiStartButton.TabIndex = 10;
            this.uiStartButton.Text = "Start";
            this.uiStartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // uiStopButton
            // 
            this.uiStopButton.Location = new System.Drawing.Point(419, 179);
            this.uiStopButton.Name = "uiStopButton";
            this.uiStopButton.Size = new System.Drawing.Size(75, 23);
            this.uiStopButton.TabIndex = 11;
            this.uiStopButton.Text = "Stop";
            this.uiStopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(254, 44);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(40, 16);
            this.label5.TabIndex = 12;
            this.label5.Text = "Status:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // uiStatus
            // 
            this.uiStatus.Location = new System.Drawing.Point(300, 44);
            this.uiStatus.Name = "uiStatus";
            this.uiStatus.Size = new System.Drawing.Size(208, 95);
            this.uiStatus.TabIndex = 13;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(12, 47);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(110, 17);
            this.label6.TabIndex = 15;
            this.label6.Text = "Current Copol Freq.";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // uiCurrentCopolFrequency
            // 
            this.uiCurrentCopolFrequency.Location = new System.Drawing.Point(128, 44);
            this.uiCurrentCopolFrequency.Name = "uiCurrentCopolFrequency";
            this.uiCurrentCopolFrequency.ReadOnly = true;
            this.uiCurrentCopolFrequency.Size = new System.Drawing.Size(100, 20);
            this.uiCurrentCopolFrequency.TabIndex = 16;
            // 
            // uiCopolChart
            // 
            chartArea5.Name = "ChartArea1";
            this.uiCopolChart.ChartAreas.Add(chartArea5);
            this.uiCopolChart.Location = new System.Drawing.Point(17, 147);
            this.uiCopolChart.Name = "uiCopolChart";
            series5.ChartArea = "ChartArea1";
            series5.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series5.Name = "Copol dBm";
            this.uiCopolChart.Series.Add(series5);
            this.uiCopolChart.Size = new System.Drawing.Size(373, 173);
            this.uiCopolChart.TabIndex = 19;
            this.uiCopolChart.Text = "chart1";
            title5.Name = "Title1";
            title5.Text = "Copol";
            this.uiCopolChart.Titles.Add(title5);
            // 
            // uiXpolChart
            // 
            chartArea6.Name = "ChartArea1";
            this.uiXpolChart.ChartAreas.Add(chartArea6);
            this.uiXpolChart.Location = new System.Drawing.Point(17, 326);
            this.uiXpolChart.Name = "uiXpolChart";
            series6.ChartArea = "ChartArea1";
            series6.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            series6.Name = "Xpol dBm";
            this.uiXpolChart.Series.Add(series6);
            this.uiXpolChart.Size = new System.Drawing.Size(373, 173);
            this.uiXpolChart.TabIndex = 19;
            this.uiXpolChart.Text = "chart1";
            title6.Name = "Title1";
            title6.Text = "Xpol";
            this.uiXpolChart.Titles.Add(title6);
            // 
            // timer
            // 
            this.timer.Interval = 1500;
            this.timer.Tick += new System.EventHandler(this.Timer_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(520, 510);
            this.Controls.Add(this.uiXpolChart);
            this.Controls.Add(this.uiCopolChart);
            this.Controls.Add(this.uiCurrentCopolFrequency);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.uiStatus);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.uiStopButton);
            this.Controls.Add(this.uiStartButton);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.uiFrequency);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.uiIsolation);
            this.Controls.Add(this.uiXpolAmplitude);
            this.Controls.Add(this.uiCopolAmplitude);
            this.Name = "MainForm";
            this.Text = "XPol";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.uiCopolChart)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.uiXpolChart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
    }
}