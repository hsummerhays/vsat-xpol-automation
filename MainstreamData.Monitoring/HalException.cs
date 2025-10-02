// <copyright file="HalException.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.ExceptionHandling
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    /// <summary>
    /// Provides a custom exception for hal errors
    /// </summary>
    [Serializable]
    public class HalException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the HalException class.
        /// </summary>
        public HalException()
        {
            // Nothing to do.  Makes parameterless constructor available.
            // Base class constructor is called by default.
        }

        /// <summary>
        /// Initializes a new instance of the HalException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public HalException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HalException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the 
        /// current exception. If the innerException parameter is not a null 
        /// reference, the current exception is raised in a catch block that 
        /// handles the inner exception.</param>
        public HalException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the HalException class.
        /// </summary>
        /// <param name="info">The System.Runtime.SerializationInfo that holds 
        /// the serialized object data bout the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext
        /// that contains contextual information about the source or destination.
        /// </param>
        protected HalException(
            SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Sets the System.Runtime.Serialization.SerializationInfo with information 
        /// about the exception.
        /// </summary>
        /// <param name="info">The System.Runtime.SerializationInfo that holds 
        /// the serialized object data bout the exception being thrown.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext
        /// that contains contextual information about the source or destination.
        /// </param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(
            SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
