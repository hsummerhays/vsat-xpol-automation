// <copyright file="ExtendedLogger.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

[assembly: System.CLSCompliant(true)]

namespace MainstreamData.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Practices.EnterpriseLibrary.Logging;

    //// TODO: Decide if want to use severity (e.g. error, information, etc.) - EnterpriseLibrary uses System.Diagnostics.TraceEventType.

    /// <summary>
    /// Priority of the message being logged.
    /// </summary>
    public enum Priority : int
    { 
        /// <summary>
        /// Low priority items a usually just informational.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Medium priority items can be followed up on when IT gets around to it.
        /// </summary>
        Medium = 3, 

        /// <summary>
        /// High priority items are usually sent to the Windows Event Log so that IT can be alerted.
        /// Note: Log Entries written by the Tracer class in EnterpriseLibrary have a hard coded Priority of 5
        /// </summary>
        High = 5 
    }

    /// <summary>
    /// Category of message being logged.
    /// </summary>
    public enum Category
    {
        /// <summary>
        /// Any configuration being done in the application can fall under this category.
        /// </summary>
        Config,

        /// <summary>
        /// Only logging that is directly related to debugging during development should use this category.
        /// </summary>
        Debug,

        /// <summary>
        /// Any exception, exception configuration exceptions, can use this category.
        /// </summary>
        Exception,

        /// <summary>
        /// A general category for anything that doesn't fit in the others.
        /// </summary>
        General,

        /// <summary>
        /// Use this category for any messages having to do with performance issues.
        /// </summary>
        Performance,

        /// <summary>
        /// Use this category for any messages having to do with security.
        /// </summary>
        Security,
    }
    
    /// <summary>
    /// Provides logging methods that use EnterpriseLibrary.Logging.Logger.
    /// In addition to the standard category logging, one high priority item
    /// within a given category is written to the CriticalNotification category,
    /// at most, once per hour.  This allows IT to be notified of problems
    /// without clogging up the Windows Event Log.
    /// </summary>
    public static class ExtendedLogger
    {
        /// <summary>
        /// A list of categories and the last time they were logged under "CriticalError".
        /// </summary>
        private static Dictionary<Category, DateTime> categoryTimeout = new Dictionary<Category, DateTime>();

        /// <summary>
        /// Allows debug messages to be ignored for MessageLogged event.
        /// </summary>
        private static bool ignoreDebugForMessageLoggedEvent = false;

        /// <summary>
        /// Fires each message is logged.
        /// </summary>
        public static event EventHandler<LogEventArgs> MessageLogged;

        /// <summary>
        /// Gets or sets a value indicating whether to ignore debug messages for the MessageLogged event.
        /// </summary>
        public static bool IgnoreDebugForMessageLoggedEvent
        {
            get 
            {
                return ExtendedLogger.ignoreDebugForMessageLoggedEvent;
            }

            set 
            {
                //// TODO: Make it so that this can be set in App.config??  
                ////    Could have it so if no trace listener is attached to the Debug category, then ignore.
                ExtendedLogger.ignoreDebugForMessageLoggedEvent = value;
            }
        }
        
        /// <summary>
        /// Adds "(error #)." to the end of a message.
        /// </summary>
        /// <param name="message">The message to add to.</param>
        /// <param name="errorNumber">The number to add.</param>
        /// <returns>A string containing the new message with the number added.</returns>
        public static string AddErrorNumberToMessage(string message, int errorNumber)
        {
            return message + " (error " + errorNumber.ToString(CultureInfo.InvariantCulture) + ").";
        }

        /// <summary>
        /// Writes an exception to the log file.
        /// </summary>
        /// <param name="message">The custom message to write.</param>
        /// <param name="category">The category to use.</param>
        /// <param name="priority">See priority enum.</param>
        /// <param name="ex">The exception object to get the details from.</param>
        public static void WriteException(string message, Category category, Priority priority, Exception ex)
        {
            ExtendedLogger.Write(
                message + "  " + ex.ToString(),
                category,
                priority);
        }

        /// <summary>
        /// Writes a debug message if a trace listener is wired to the Debug category.
        /// </summary>
        /// <param name="message">The custom message to write.</param>
        public static void WriteDebug(string message)
        {
            ExtendedLogger.Write(message, Category.Debug, Priority.Low);
        }

        /// <summary>
        /// Writes a message to the log file.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="category">The category to use.</param>
        /// <param name="priority">See priority enum.</param>
        public static void Write(string message, Category category, Priority priority)
        {
            // Get category string
            string categoryString = Enum.GetName(typeof(Category), category);

            // Log to "CriticalError" category if priority is high.
            if (priority.Equals(Priority.High))
            {   
                // See if category has a stored time
                DateTime lastPriorityLog = new DateTime();
                if (categoryTimeout.ContainsKey(category))
                {
                    lastPriorityLog = categoryTimeout[category];
                }
                else
                {
                    categoryTimeout.Add(category, DateTime.Now);
                }

                // If high priority, then see if should log to EventLog using category "Critical".
                if (lastPriorityLog < DateTime.Now.AddMinutes(-60))
                {
                    Logger.Write(
                        categoryString + ": " + message,
                        "CriticalNotification",
                        (int)priority,
                        0,
                        TraceEventType.Error);
                    categoryTimeout[category] = DateTime.Now;
                }
            }

            // Log to normal log
            //// TODO: Attempt to write logging errors (such as file access denied) to local folder and/or EventLog.
            //// TODO: Make it so that Category.Exception is written with TraceEventType.Error instead of default of Information.
            Logger.Write(message, categoryString, (int)priority);

            // TODO: See LogEventArgs TODO - Create log entry and event args to pass to event.
            ////LogEntry logEntry = new LogEntry();
            ////logEntry.Message = message;
            ////logEntry.Categories.Add(categoryString);
            ////logEntry.Priority = (int)priority;

            if (category != Category.Debug || !ExtendedLogger.ignoreDebugForMessageLoggedEvent)
            {
                LogEventArgs logEventArgs = new LogEventArgs(message, categoryString, priority);

                // Fire event handler
                // TODO: Use reflection to provide reference to ExtendedLogger
                object emptyObject = new object();
                if (ExtendedLogger.MessageLogged != null)
                {
                    ExtendedLogger.MessageLogged(emptyObject, logEventArgs);
                }
            }
        }
    }
}
