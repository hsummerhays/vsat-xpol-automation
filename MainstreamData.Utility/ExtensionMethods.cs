// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>
// -----------------------------------------------------------------------

namespace MainstreamData.Utility
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Contains extension methods for this namespace.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets the current read position of the StreamReader.
        /// </summary>
        /// <param name="streamReader">The StreamReader object to get the position for.</param>
        /// <returns>Current read position in the StreamReader.</returns>
        public static int GetPosition(this StreamReader streamReader)
        {
            // Based on code shared on www.daniweb.com by user mfm24(Matt).
            int charpos = (int)streamReader.GetType().InvokeMember(
                "charPos", 
                BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                null,
                streamReader,
                null,
                CultureInfo.InvariantCulture);
            int charlen = (int)streamReader.GetType().InvokeMember(
                "charLen", 
                BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField,
                null,
                streamReader,
                null,
                CultureInfo.InvariantCulture);
            return (int)streamReader.BaseStream.Position - charlen + charpos;
        }

        /// <summary>
        /// Sets the current read position of the StreamReader.
        /// </summary>
        /// <param name="streamReader">The StreamReader object to get the position for.</param>
        /// <param name="position">The position to move to in the file, starting from the beginning.</param>
        public static void SetPosition(this StreamReader streamReader, long position)
        {
            streamReader.BaseStream.Seek(position, SeekOrigin.Begin);
            streamReader.DiscardBufferedData();
        }

        /// <summary>
        /// Checks if the delegate has any listeners and then calls their methods.
        /// </summary>
        /// <param name="handler">The event object to handle.</param>
        /// <param name="sourceObject">The object containing the event.</param>
        /// <returns>True if cancelled.</returns>
        public static bool SafeInvoke(this EventHandler<CancelEventArgs> handler, object sourceObject)
        {
            // TODO: Add code to continue calling remaining listeners even if there is an exception.
            // Call BufferFull event
            bool cancel = false;
            if (handler != null)
            {
                CancelEventArgs args = new CancelEventArgs();
                handler.Invoke(sourceObject, args);
                cancel = args.Cancel;
            }
            else
            {
                cancel = true;
            }

            return cancel;
        }

        /// <summary>
        /// Checks if the delegate has any listeners and then calls their methods.
        /// </summary>
        /// <param name="handler">The event object to handle.</param>
        /// <param name="sourceObject">The object containing the event.</param>
        public static void SafeInvoke(this EventHandler<EventArgs> handler, object sourceObject)
        {
            // TODO: Add code to continue calling remaining listeners even if there is an exception.
            if (handler != null)
            {
                handler.Invoke(sourceObject, new EventArgs());
            }
        }

        /// <summary>
        /// Checks to see if a value is contained within other values (used like 1.In(1,2,3)).
        /// </summary>
        /// <typeparam name="T">The type of the value being checked.</typeparam>
        /// <param name="val">The value to check.</param>
        /// <param name="values">The values to check against.</param>
        /// <returns>True if the value is contained within the other values.</returns>
        public static bool In<T>(this T val, params T[] values) where T : struct
        {
            return values.Contains(val);
        }

        /// <summary>
        /// Clears the contents of the string builder but doesn't reset the capacity.
        /// </summary>
        /// <param name="value">The <see cref="StringBuilder"/> to clear.</param>
        public static void Clear(this StringBuilder value)
        {
            value.Length = 0;
        }
    }
}
