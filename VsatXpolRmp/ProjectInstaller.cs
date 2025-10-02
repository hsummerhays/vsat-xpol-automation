// <copyright file="ProjectInstaller.cs" company="Mainstream Data, Inc.">
// Copyright Mainstream Data, Inc.
// </copyright>

namespace MainstreamData.Monitoring.VsatXpol.VsatXpolRmp
{
    using System.ComponentModel;
    using System.Configuration.Install;

    /// <summary>
    /// Installs the service.
    /// </summary>
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        /// <summary>
        /// Initializes a new instance of the ProjectInstaller class.
        /// </summary>
        public ProjectInstaller()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Called after the service is installed.
        /// </summary>
        /// <param name="sender">The object that called this method.</param>
        /// <param name="e">The arguments for this method.</param>
        private void ServiceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
        }
    }
}
