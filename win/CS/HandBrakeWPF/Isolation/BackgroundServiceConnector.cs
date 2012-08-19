﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BackgroundServiceConnector.cs" company="HandBrake Project (http://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   Background Service Connector.
//   HandBrake has the ability to connect to a service app that will control HandBrakeCLI or Libhb.
//   This acts as process isolation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.Isolation
{
    using System;
    using System.Diagnostics;
    using System.ServiceModel;
    using System.Threading;

    using HandBrake.ApplicationServices.EventArgs;
    using HandBrake.ApplicationServices.Services.Interfaces;

    using HandBrakeWPF.Services.Interfaces;

    /// <summary>
    /// Background Service Connector.
    /// HandBrake has the ability to connect to a service app that will control HandBrakeCLI or Libhb. 
    /// This acts as process isolation.
    /// </summary>
    public class BackgroundServiceConnector : IHbServiceCallback, IDisposable
    {
        /// <summary>
        /// The error service.
        /// </summary>
        private readonly IErrorService errorService;

        #region Constants and Fields

        /// <summary>
        /// Gets or sets the pipe factory.
        /// DuplexChannelFactory is necessary for Callbacks.
        /// </summary>
        private DuplexChannelFactory<IServerService> pipeFactory;

        /// <summary>
        /// The background process.
        /// </summary>
        private Process backgroundProcess;

        #endregion

        #region Properties

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundServiceConnector"/> class.
        /// </summary>
        /// <param name="errorService">
        /// The error service.
        /// </param>
        public BackgroundServiceConnector(IErrorService errorService)
        {
            this.errorService = errorService;
        }

        /// <summary>
        /// Gets or sets a value indicating whether is connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the service.
        /// </summary>
        public IServerService Service { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// The can connect.
        /// </summary>
        /// <returns>
        /// The System.Boolean.
        /// </returns>
        public bool CanConnect()
        {
            return true;
        }

        /// <summary>
        /// The connect.
        /// </summary>
        public void Connect()
        {
            ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        this.pipeFactory = new DuplexChannelFactory<IServerService>(
                            new InstanceContext(this),
                            new NetTcpBinding(),
                            new EndpointAddress("net.tcp://127.0.0.1:8000/IHbService"));

                        // Connect and Subscribe to the Server
                        this.Service = this.pipeFactory.CreateChannel();
                        this.Service.Subscribe();
                        this.IsConnected = true;
                    }
                    catch (Exception exc)
                    {
                        Caliburn.Micro.Execute.OnUIThread(() => this.errorService.ShowError("Unable to connect to background worker service", "Please restart HandBrake", exc));
                    }
                });
        }

        /// <summary>
        /// The disconnect.
        /// </summary>
        public void Disconnect()
        {
            if (backgroundProcess != null && !backgroundProcess.HasExited)
            {
                try
                {
                    this.Service.Unsubscribe();
                }
                catch (Exception exc)
                {
                    this.errorService.ShowError("Unable to disconnect from service", "It may have already close. Check for any left over HandBrake.Server.exe processes", exc);
                }
            }
        }

        /// <summary>
        /// The scan source.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="title">
        /// The title.
        /// </param>
        /// <param name="previewCount">
        /// The preview count.
        /// </param>
        public void ScanSource(string path, int title, int previewCount)
        {
            ThreadPool.QueueUserWorkItem(delegate { this.Service.ScanSource(path, title, previewCount); });
        }

        /// <summary>
        /// The start server.
        /// </summary>
        public void StartServer()
        {
            if (this.backgroundProcess == null)
            {
                this.backgroundProcess = Process.Start("HandBrake.Server.exe");
            }
        }

        #endregion

        #region Implemented Interfaces

        #region IDisposable

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.Service.Unsubscribe();
        }

        #endregion

        #region IHbServiceCallback

        /// <summary>
        /// The scan completed.
        /// </summary>
        /// <param name="eventArgs">
        /// The event args.
        /// </param>
        public virtual void ScanCompletedCallback(ScanCompletedEventArgs eventArgs)
        {
        }

        /// <summary>
        /// The scan progress.
        /// </summary>
        /// <param name="eventArgs">
        /// The event args.
        /// </param>
        public virtual void ScanProgressCallback(ScanProgressEventArgs eventArgs)
        {
        }

        /// <summary>
        /// The scan started callback.
        /// </summary>
        public virtual void ScanStartedCallback()
        {
        }

        #endregion

        #endregion

        #region Implementation of IHbServiceCallback

        /// <summary>
        /// The test.
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        public void Test(string message)
        {
            Console.WriteLine(message);
        }

        #endregion
    }
}