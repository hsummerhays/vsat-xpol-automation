// <copyright file="LogEventArgs.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Logging
{
    using System;

    // TODO: See about making it so that this class uses EnterpriseLibrary.Logging.LogEntry - was getting stack overflow errors.

    /// <summary>
    /// Provides details about logged events.
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the LogEventArgs class.
        /// </summary>
        public LogEventArgs() :
            base()
        {
            // Simply making default constructor public
        }

        /// <summary>
        /// Initializes a new instance of the LogEventArgs class with the specified information.
        /// </summary>
        /// <param name="message">The message to store.</param>
        /// <param name="category">The category to store.</param>
        /// <param name="priority">The priority to store.</param>
        public LogEventArgs(string message, string category, Priority priority)
        {
            this.Message = message;
            this.Category = category;
            this.Priority = priority;
        }

        /// <summary>
        /// Gets or sets the message for the event.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the category for the event.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the priority for the event.
        /// </summary>
        public Priority Priority { get; set; }
    }
}