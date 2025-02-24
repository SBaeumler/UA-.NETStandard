/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Opc.Ua.Configuration
{
    /// <summary>
    /// Interface to create application callbacks.
    /// </summary>
    public abstract class IApplicationMessageDlg
    {
        /// <summary>
        /// The application message.
        /// </summary>
        /// <param name="text">The text of the message.</param>
        /// <param name="ask">If the application should ask the user.</param>
        public abstract void Message(string text, Boolean ask = false);

        /// <summary>
        /// Show the message and return result.
        /// </summary>
        public abstract Task<bool> ShowAsync();
    }

    /// <summary>
    /// A class that install, configures and runs a UA application.
    /// </summary>
    public class ApplicationInstance
    {
        #region Ctors
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInstance"/> class.
        /// </summary>
        public ApplicationInstance()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInstance"/> class.
        /// </summary>
        /// <param name="applicationConfiguration">The application configuration.</param>
        public ApplicationInstance(ApplicationConfiguration applicationConfiguration)
        {
            m_applicationConfiguration = applicationConfiguration;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        public string ApplicationName
        {
            get { return m_applicationName; }
            set { m_applicationName = value; }
        }

        /// <summary>
        /// Gets or sets the type of the application.
        /// </summary>
        /// <value>The type of the application.</value>
        public ApplicationType ApplicationType
        {
            get { return m_applicationType; }
            set { m_applicationType = value; }
        }

        /// <summary>
        /// Gets or sets the name of the config section containing the path to the application configuration file.
        /// </summary>
        /// <value>The name of the config section.</value>
        public string ConfigSectionName
        {
            get { return m_configSectionName; }
            set { m_configSectionName = value; }
        }

        /// <summary>
        /// Gets or sets the type of configuration file.
        /// </summary>
        /// <value>The type of configuration file.</value>
        public Type ConfigurationType
        {
            get { return m_configurationType; }
            set { m_configurationType = value; }
        }

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>The server.</value>
        public ServerBase Server => m_server;

        /// <summary>
        /// Gets the application configuration used when the Start() method was called.
        /// </summary>
        /// <value>The application configuration.</value>
        public ApplicationConfiguration ApplicationConfiguration
        {
            get { return m_applicationConfiguration; }
            set { m_applicationConfiguration = value; }
        }

        /// <summary>
        /// Gets or sets a flag that indicates whether the application will be set up for management with the GDS agent.
        /// </summary>
        /// <value>If true the application will not be visible to the GDS local agent after installation.</value>
        public bool NoGdsAgentAdmin { get; set; }

        /// <summary>
        /// Get or set the message dialog.
        /// </summary>
        public static IApplicationMessageDlg MessageDlg { get; set; }

        /// <summary>
        /// Get or set the certificate password provider.
        /// </summary>
        public ICertificatePasswordProvider CertificatePasswordProvider { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Processes the command line.
        /// </summary>
        /// <returns>
        /// True if the arguments were processed; False otherwise.
        /// </returns>
        public bool ProcessCommandLine()
        {
            // ignore processing of command line
            return false;
        }

        /// <summary>
        /// Starts the UA server as a Windows Service.
        /// </summary>
        /// <param name="server">The server.</param>
        public void StartAsService(ServerBase server)
        {
            throw new NotImplementedException(".NetStandard Opc.Ua libraries do not support to start as a windows service");
        }

        /// <summary>
        /// Starts the UA server.
        /// </summary>
        /// <param name="server">The server.</param>
        public async Task Start(ServerBase server)
        {
            m_server = server;

            if (m_applicationConfiguration == null)
            {
                await LoadApplicationConfiguration(false).ConfigureAwait(false);
            }

            server.Start(m_applicationConfiguration);
        }

        /// <summary>
        /// Stops the UA server.
        /// </summary>
        public void Stop()
        {
            m_server.Stop();
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Helper to replace localhost with the hostname
        /// in the application uri and base adresses of the
        /// configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public static ApplicationConfiguration FixupAppConfig(
            ApplicationConfiguration configuration)
        {
            configuration.ApplicationUri = Utils.ReplaceLocalhost(configuration.ApplicationUri);
            if (configuration.ServerConfiguration != null)
            {
                for (int i = 0; i < configuration.ServerConfiguration.BaseAddresses.Count; i++)
                {
                    configuration.ServerConfiguration.BaseAddresses[i] =
                        Utils.ReplaceLocalhost(configuration.ServerConfiguration.BaseAddresses[i]);
                }
            }
            return configuration;
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        public static async Task<ApplicationConfiguration> LoadAppConfig(
            bool silent,
            string filePath,
            ApplicationType applicationType,
            Type configurationType,
            bool applyTraceSettings,
            ICertificatePasswordProvider certificatePasswordProvider = null)
        {
            Utils.Trace(Utils.TraceMasks.Information, "Loading application configuration file. {0}", filePath);

            try
            {
                // load the configuration file.
                ApplicationConfiguration configuration = await ApplicationConfiguration.Load(
                    new System.IO.FileInfo(filePath),
                    applicationType,
                    configurationType,
                    applyTraceSettings,
                    certificatePasswordProvider)
                    .ConfigureAwait(false);

                if (configuration == null)
                {
                    return null;
                }

                return configuration;
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Could not load configuration file. {0}", filePath);

                // warn user.
                if (!silent)
                {
                    if (MessageDlg != null)
                    {
                        MessageDlg.Message("Load Application Configuration: " + e.Message);
                        await MessageDlg.ShowAsync().ConfigureAwait(false);
                    }

                    throw;
                }

                return null;
            }
        }

        /// <summary>
        /// Loads the application configuration.
        /// </summary>
        public async Task<ApplicationConfiguration> LoadApplicationConfiguration(string filePath, bool silent)
        {
            ApplicationConfiguration configuration = null;

            try
            {
                configuration = await LoadAppConfig(
                    silent, filePath, ApplicationType, ConfigurationType, true, CertificatePasswordProvider)
                    .ConfigureAwait(false);
            }
            catch (Exception) when (silent)
            {
            }

            if (configuration == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadConfigurationError, "Could not load configuration file.");
            }

            m_applicationConfiguration = FixupAppConfig(configuration);

            return configuration;
        }

        /// <summary>
        /// Loads the application configuration.
        /// </summary>
        public async Task<ApplicationConfiguration> LoadApplicationConfiguration(bool silent)
        {
            string filePath = ApplicationConfiguration.GetFilePathFromAppConfig(ConfigSectionName);

            return await LoadApplicationConfiguration(filePath, silent).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks for a valid application instance certificate.
        /// </summary>
        /// <param name="silent">if set to <c>true</c> no dialogs will be displayed.</param>
        /// <param name="minimumKeySize">Minimum size of the key.</param>
        public Task<bool> CheckApplicationInstanceCertificate(
            bool silent,
            ushort minimumKeySize)
        {
            return CheckApplicationInstanceCertificate(silent, minimumKeySize, CertificateFactory.DefaultLifeTime);
        }

        /// <summary>
        /// Checks for a valid application instance certificate.
        /// </summary>
        /// <param name="silent">if set to <c>true</c> no dialogs will be displayed.</param>
        /// <param name="minimumKeySize">Minimum size of the key.</param>
        /// <param name="lifeTimeInMonths">The lifetime in months.</param>
        public async Task<bool> CheckApplicationInstanceCertificate(
            bool silent,
            ushort minimumKeySize,
            ushort lifeTimeInMonths)
        {
            Utils.Trace(Utils.TraceMasks.Information, "Checking application instance certificate.");
            if (m_applicationConfiguration == null)
            {
                await LoadApplicationConfiguration(silent).ConfigureAwait(false);
            }

            ApplicationConfiguration configuration = m_applicationConfiguration;
            bool certificateValid = false;

            // find the existing certificate.
            CertificateIdentifier id = configuration.SecurityConfiguration.ApplicationCertificate;

            if (id == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                    "Configuration file does not specify a certificate.");
            }

            X509Certificate2 certificate = await id.Find(true).ConfigureAwait(false);

            // check that it is ok.
            if (certificate != null)
            {
                certificateValid = await CheckApplicationInstanceCertificate(configuration, certificate, silent, minimumKeySize).ConfigureAwait(false);
            }
            else
            {
                // check for missing private key.
                certificate = await id.Find(false).ConfigureAwait(false);

                if (certificate != null)
                {
                    throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                        "Cannot access certificate private key. Subject={0}", certificate.Subject);
                }

                // check for missing thumbprint.
                if (!String.IsNullOrEmpty(id.Thumbprint))
                {
                    if (!String.IsNullOrEmpty(id.SubjectName))
                    {
                        CertificateIdentifier id2 = new CertificateIdentifier();
                        id2.StoreType = id.StoreType;
                        id2.StorePath = id.StorePath;
                        id2.SubjectName = id.SubjectName;
                        certificate = await id2.Find(true).ConfigureAwait(false);
                    }

                    if (certificate != null)
                    {
                        var message = new StringBuilder();
                        message.AppendLine("Thumbprint was explicitly specified in the configuration.");
                        message.AppendLine("Another certificate with the same subject name was found.");
                        message.AppendLine("Use it instead?");
                        message.AppendLine("Requested: {0}");
                        message.AppendLine("Found: {1}");
                        if (!await ApproveMessage(String.Format(message.ToString(), id.SubjectName, certificate.Subject), silent).ConfigureAwait(false))
                        {
                            throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                                message.ToString(), id.SubjectName, certificate.Subject);
                        }
                    }
                    else
                    {
                        var message = new StringBuilder();
                        message.AppendLine("Thumbprint was explicitly specified in the configuration. ");
                        message.AppendLine("Cannot generate a new certificate.");
                        throw ServiceResultException.Create(StatusCodes.BadConfigurationError, message.ToString());
                    }
                }
            }

            if ((certificate == null) || !certificateValid)
            {
                certificate = await CreateApplicationInstanceCertificate(configuration,
                    minimumKeySize, lifeTimeInMonths).ConfigureAwait(false);

                if (certificate == null)
                {
                    var message = new StringBuilder();
                    message.AppendLine("There is no cert with subject {0} in the configuration.");
                    message.AppendLine(" Please generate a cert for your application,");
                    message.AppendLine(" then copy the new cert to this location:");
                    message.AppendLine(" {1}");
                    throw ServiceResultException.Create(StatusCodes.BadConfigurationError,
                        message.ToString(), id.SubjectName, id.StorePath
                        );
                }
            }
            else
            {
                if (configuration.SecurityConfiguration.AddAppCertToTrustedStore)
                {
                    // ensure it is trusted.
                    await AddToTrustedStore(configuration, certificate).ConfigureAwait(false);
                }
            }

            return true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates an application instance certificate if one does not already exist.
        /// </summary>
        private static async Task<bool> CheckApplicationInstanceCertificate(
            ApplicationConfiguration configuration,
            X509Certificate2 certificate,
            bool silent,
            ushort minimumKeySize)
        {
            if (certificate == null)
            {
                return false;
            }

            Utils.Trace(Utils.TraceMasks.Information, "Checking application instance certificate. {0}", certificate.Subject);

            try
            {
                // validate certificate.
                configuration.CertificateValidator.Validate(certificate);
            }
            catch (Exception ex)
            {
                string message = Utils.Format(
                    "Error validating certificate. Exception: {0}. Use certificate anyway?", ex.Message);
                if (!await ApproveMessage(message, silent).ConfigureAwait(false))
                {
                    return false;
                }
            }

            // check key size.
            int keySize = X509Utils.GetRSAPublicKeySize(certificate);
            if (minimumKeySize > keySize)
            {
                string message = Utils.Format(
                    "The key size ({0}) in the certificate is less than the minimum provided ({1}). Use certificate anyway?",
                    keySize,
                    minimumKeySize);

                if (!await ApproveMessage(message, silent).ConfigureAwait(false))
                {
                    return false;
                }
            }

            // check domains.
            if (configuration.ApplicationType != ApplicationType.Client)
            {
                if (!await CheckDomainsInCertificate(configuration, certificate, silent).ConfigureAwait(false))
                {
                    return false;
                }
            }

            // check uri.
            string applicationUri = X509Utils.GetApplicationUriFromCertificate(certificate);

            if (String.IsNullOrEmpty(applicationUri))
            {
                string message = "The Application URI could not be read from the certificate. Use certificate anyway?";
                if (!await ApproveMessage(message, silent).ConfigureAwait(false))
                {
                    return false;
                }
            }
            else
            {
                configuration.ApplicationUri = applicationUri;
            }

            // update configuration.
            configuration.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;

            return true;
        }

        /// <summary>
        /// Checks that the domains in the server addresses match the domains in the certificates.
        /// </summary>
        private static async Task<bool> CheckDomainsInCertificate(
            ApplicationConfiguration configuration,
            X509Certificate2 certificate,
            bool silent)
        {
            Utils.Trace(Utils.TraceMasks.Information, "Checking domains in certificate. {0}", certificate.Subject);

            bool valid = true;
            IList<string> serverDomainNames = configuration.GetServerDomainNames();
            IList<string> certificateDomainNames = X509Utils.GetDomainsFromCertficate(certificate);

            // get computer name.
            string computerName = Utils.GetHostName();

            // get IP addresses.
            IPAddress[] addresses = null;

            for (int ii = 0; ii < serverDomainNames.Count; ii++)
            {
                if (Utils.FindStringIgnoreCase(certificateDomainNames, serverDomainNames[ii]))
                {
                    continue;
                }

                if (String.Equals(serverDomainNames[ii], "localhost", StringComparison.OrdinalIgnoreCase))
                {
                    if (Utils.FindStringIgnoreCase(certificateDomainNames, computerName))
                    {
                        continue;
                    }

                    // check for aliases.
                    bool found = false;

                    // get IP addresses only if necessary.
                    if (addresses == null)
                    {
                        addresses = await Utils.GetHostAddressesAsync(computerName).ConfigureAwait(false);
                    }

                    // check for ip addresses.
                    for (int jj = 0; jj < addresses.Length; jj++)
                    {
                        if (Utils.FindStringIgnoreCase(certificateDomainNames, addresses[jj].ToString()))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        continue;
                    }
                }

                string message = Utils.Format(
                    "The server is configured to use domain '{0}' which does not appear in the certificate. Use certificate anyway?",
                    serverDomainNames[ii]);

                valid = false;

                if (await ApproveMessage(message, silent).ConfigureAwait(false))
                {
                    valid = true;
                    continue;
                }

                break;
            }

            return valid;
        }

        /// <summary>
        /// Creates the application instance certificate.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="keySize">Size of the key.</param>
        /// <param name="lifeTimeInMonths">The lifetime in months.</param>
        /// <returns>The new certificate</returns>
        private static async Task<X509Certificate2> CreateApplicationInstanceCertificate(
            ApplicationConfiguration configuration,
            ushort keySize,
            ushort lifeTimeInMonths
            )
        {
            Utils.Trace(Utils.TraceMasks.Information, "Creating application instance certificate.");

            // delete any existing certificate.
            await DeleteApplicationInstanceCertificate(configuration).ConfigureAwait(false);

            CertificateIdentifier id = configuration.SecurityConfiguration.ApplicationCertificate;

            // get the domains from the configuration file.
            IList<string> serverDomainNames = configuration.GetServerDomainNames();

            if (serverDomainNames.Count == 0)
            {
                serverDomainNames.Add(Utils.GetHostName());
            }

            // ensure the certificate store directory exists.
            if (id.StoreType == CertificateStoreType.Directory)
            {
                Utils.GetAbsoluteDirectoryPath(id.StorePath, true, true, true);
            }

            X509Certificate2 certificate = CertificateFactory.CreateCertificate(
                configuration.ApplicationUri,
                configuration.ApplicationName,
                id.SubjectName,
                serverDomainNames)
                .SetLifeTime(lifeTimeInMonths)
                .SetRSAKeySize(keySize)
                .CreateForRSA();

            id.Certificate = certificate;
            var passwordProvider = configuration.SecurityConfiguration.CertificatePasswordProvider;
            certificate.AddToStore(
                id.StoreType,
                id.StorePath,
                passwordProvider?.GetPassword(id)
                );

            // ensure the certificate is trusted.
            if (configuration.SecurityConfiguration.AddAppCertToTrustedStore)
            {
                await AddToTrustedStore(configuration, certificate).ConfigureAwait(false);
            }

            await configuration.CertificateValidator.Update(configuration.SecurityConfiguration).ConfigureAwait(false);

            Utils.Trace(Utils.TraceMasks.Information, "Certificate created. Thumbprint={0}", certificate.Thumbprint);

            // reload the certificate from disk.
            await configuration.SecurityConfiguration.ApplicationCertificate.LoadPrivateKeyEx(passwordProvider).ConfigureAwait(false);

            return certificate;
        }

        /// <summary>
        /// Deletes an existing application instance certificate.
        /// </summary>
        /// <param name="configuration">The configuration instance that stores the configurable information for a UA application.</param>
        private static async Task DeleteApplicationInstanceCertificate(ApplicationConfiguration configuration)
        {
            Utils.Trace(Utils.TraceMasks.Information, "Deleting application instance certificate.");

            // create a default certificate id none specified.
            CertificateIdentifier id = configuration.SecurityConfiguration.ApplicationCertificate;

            if (id == null)
            {
                return;
            }

            // delete private key.
            X509Certificate2 certificate = await id.Find().ConfigureAwait(false);

            // delete trusted peer certificate.
            if (configuration.SecurityConfiguration != null && configuration.SecurityConfiguration.TrustedPeerCertificates != null)
            {
                string thumbprint = id.Thumbprint;

                if (certificate != null)
                {
                    thumbprint = certificate.Thumbprint;
                }

                using (ICertificateStore store = configuration.SecurityConfiguration.TrustedPeerCertificates.OpenStore())
                {
                    await store.Delete(thumbprint).ConfigureAwait(false);
                }
            }

            // delete private key.
            if (certificate != null)
            {
                using (ICertificateStore store = id.OpenStore())
                {
                    await store.Delete(certificate.Thumbprint).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Adds the certificate to the Trusted Certificate Store
        /// </summary>
        /// <param name="configuration">The application's configuration which specifies the location of the TrustedStore.</param>
        /// <param name="certificate">The certificate to register.</param>
        private static async Task AddToTrustedStore(ApplicationConfiguration configuration, X509Certificate2 certificate)
        {
            if (certificate == null) throw new ArgumentNullException(nameof(certificate));

            string storePath = null;

            if (configuration != null && configuration.SecurityConfiguration != null && configuration.SecurityConfiguration.TrustedPeerCertificates != null)
            {
                storePath = configuration.SecurityConfiguration.TrustedPeerCertificates.StorePath;
            }

            if (String.IsNullOrEmpty(storePath))
            {
                Utils.Trace(Utils.TraceMasks.Information, "WARNING: Trusted peer store not specified.");
                return;
            }

            try
            {
                ICertificateStore store = configuration.SecurityConfiguration.TrustedPeerCertificates.OpenStore();

                if (store == null)
                {
                    Utils.Trace("Could not open trusted peer store. StorePath={0}", storePath);
                    return;
                }

                try
                {
                    // check if it already exists.
                    X509Certificate2Collection existingCertificates = await store.FindByThumbprint(certificate.Thumbprint).ConfigureAwait(false);

                    if (existingCertificates.Count > 0)
                    {
                        return;
                    }

                    Utils.Trace(Utils.TraceMasks.Information, "Adding certificate to trusted peer store. StorePath={0}", storePath);

                    List<string> subjectName = X509Utils.ParseDistinguishedName(certificate.Subject);

                    // check for old certificate.
                    X509Certificate2Collection certificates = await store.Enumerate().ConfigureAwait(false);

                    for (int ii = 0; ii < certificates.Count; ii++)
                    {
                        if (X509Utils.CompareDistinguishedName(certificates[ii], subjectName))
                        {
                            if (certificates[ii].Thumbprint == certificate.Thumbprint)
                            {
                                return;
                            }

                            await store.Delete(certificates[ii].Thumbprint).ConfigureAwait(false);
                            break;
                        }
                    }

                    // add new certificate.
                    X509Certificate2 publicKey = new X509Certificate2(certificate.RawData);
                    await store.Add(publicKey).ConfigureAwait(false);
                }
                finally
                {
                    store.Close();
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Could not add certificate to trusted peer store. StorePath={0}", storePath);
            }
        }

        /// <summary>
        /// Show a message for approval and return result.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="silent"></param>
        /// <returns>True if approved, false otherwise.</returns>
        private static async Task<bool> ApproveMessage(string message, bool silent)
        {
            if (!silent && MessageDlg != null)
            {
                MessageDlg.Message(message, true);
                return await MessageDlg.ShowAsync().ConfigureAwait(false);
            }
            else
            {
                Utils.Trace(message);
                return false;
            }
        }
        #endregion

        #region Private Fields
        private string m_applicationName;
        private ApplicationType m_applicationType;
        private string m_configSectionName;
        private Type m_configurationType;
        private ServerBase m_server;
        private ApplicationConfiguration m_applicationConfiguration;
        #endregion
    }
}
