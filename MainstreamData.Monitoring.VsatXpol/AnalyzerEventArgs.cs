// -----------------------------------------------------------------------
// <copyright file="AnalyzerEventArgs.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>
// -----------------------------------------------------------------------

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Monitoring.VsatXpol
{
    using System;
    using MainstreamData.Monitoring;

    /// <summary>
    /// Holds arguments for SpecAnalyzer events.
    /// </summary>
    public class AnalyzerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the AnalyzerEventArgs class.
        /// </summary>
        /// <param name="message">The message to pass to the event handler.</param>
        public AnalyzerEventArgs(string message) :
            this(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AnalyzerEventArgs class.
        /// </summary>
        /// <param name="signal">The current signal being updated.</param>
        public AnalyzerEventArgs(Signal signal) :
            this(string.Empty, signal)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AnalyzerEventArgs class.
        /// </summary>
        /// <param name="message">The message to pass to the event handler.</param>
        /// <param name="signal">The current Signal being updated.</param>
        public AnalyzerEventArgs(string message, Signal signal) :
            base()
        {
            this.Message = message;
            this.Signal = signal;
        }

        /// <summary>
        /// Gets or sets the message to pass to the event handler.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the current Signal being updated.
        /// </summary>
        public Signal Signal { get; set; }
    }
}