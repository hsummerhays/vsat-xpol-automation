// <copyright file="EmailItem.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.MSExchange
{
    /// <summary>
    /// Holds information about a single email.
    /// </summary>
    public struct EmailItem
    {
        /// <summary>
        /// Unique identifier for an email message.
        /// </summary>
        public string Id;

        /// <summary>
        /// Subject line of the email message.
        /// </summary>
        public string Subject;

        /// <summary>
        /// Body of the email message.
        /// </summary>
        public string Body;
    }
}
