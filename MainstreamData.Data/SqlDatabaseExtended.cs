// <copyright file="SqlDatabaseExtended.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Data
{
    using System.Data.SqlClient;
    using Microsoft.Practices.EnterpriseLibrary.Data.Sql;

    /// <summary>
    /// Provides a wrapper around the Microsoft Enterprise Library Data block's
    /// SqlDatabase class to allow it to be setup by a ConnectionStringBuilder
    /// </summary>
    public class SqlDatabaseExtended : SqlDatabase
    {
        /// <summary>
        /// Initializes a new instance of the SqlDatabaseExtended class
        /// </summary>
        /// <param name="connectionStringBuilder">A connection string builder 
        /// object that contains info needed to connect to the database</param>
        public SqlDatabaseExtended(SqlConnectionStringBuilder connectionStringBuilder)
            : base(connectionStringBuilder.ToString())
        {
            // Nothing to do here, but call base constructor
        }

        /// <summary>
        /// Initializes a new instance of the SqlDatabaseExtended class
        /// </summary>
        /// <param name="connectionString">A SQL connection string containing 
        /// info needed to connect to the database</param>
        public SqlDatabaseExtended(string connectionString)
            : base(connectionString)
        {
            // Nothing to do here, but call base constructor
        }
    }
}
