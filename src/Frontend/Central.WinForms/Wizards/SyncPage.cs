﻿/*
 * Copyright 2010-2015 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NanoByte.Common.Controls;
using NanoByte.Common.Tasks;
using ZeroInstall.Central.Properties;
using ZeroInstall.Commands;
using ZeroInstall.Commands.CliCommands;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.Services;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store;

namespace ZeroInstall.Central.WinForms.Wizards
{
    /// <summary>
    /// Base class for <see cref="Wizard"/> pages that need a <see cref="SyncApps"/>.
    /// </summary>
    internal partial class SyncPage : UserControl, ITaskHandler
    {
        protected readonly bool MachineWide;

        public SyncPage()
        {
            InitializeComponent();
            labelWorking.Text = Resources.Working;
        }

        public SyncPage(bool machineWide) : this()
        {
            MachineWide = machineWide;
        }

        #region Create sync
        /// <summary>
        /// Creates a new <see cref="SyncIntegrationManager"/> using a custom crypto key.
        /// </summary>
        /// <param name="cryptoKey">The crypto key to use; overrides <see cref="Config.SyncCryptoKey"/>.</param>
        /// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
        /// <returns>A new <see cref="SyncIntegrationManager"/> instance.</returns>
        protected SyncIntegrationManager CreateSync(string cryptoKey, bool machineWide)
        {
            var services = new ServiceLocator(this);
            return new SyncIntegrationManager(services.Config.ToSyncServer(), cryptoKey, services.FeedManager.GetFeedFresh, services.Handler, machineWide);
        }

        /// <summary>
        /// Creates a new <see cref="SyncIntegrationManager"/> using a custom server and credentials.
        /// </summary>
        /// <param name="server">Access information for the sync server; overrides <see cref="Config"/>.</param>
        /// <param name="cryptoKey">The crypto key to use; overrides <see cref="Config.SyncCryptoKey"/>; overrides <see cref="Config.SyncCryptoKey"/>.</param>
        /// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
        /// <returns>A new <see cref="SyncIntegrationManager"/> instance.</returns>
        protected SyncIntegrationManager CreateSync(SyncServer server, string cryptoKey, bool machineWide)
        {
            var services = new ServiceLocator(this);
            return new SyncIntegrationManager(server, cryptoKey, services.FeedManager.GetFeedFresh, services.Handler, machineWide);
        }
        #endregion

        #region ITaskHandler
        /// <summary>
        /// Should never be signaled.
        /// </summary>
        public CancellationToken CancellationToken { get { return default(CancellationToken); } }

        /// <summary>
        /// Always returns <see cref="NanoByte.Common.Tasks.Verbosity.Batch"/>
        /// </summary>
        public Verbosity Verbosity { get { return Verbosity.Batch; } set { } }

        /// <inheritdoc/>
        public void RunTask(ITask task)
        {
            Invoke(new Action(() => { using (var handler = new GuiTaskHandler(this)) handler.RunTask(task); }));
        }

        /// <summary>
        /// Always returns <see langword="false"/>.
        /// </summary>
        public bool Ask(string question)
        {
            return false;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Output(string title, string message)
        {}

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Output<T>(string title, IEnumerable<T> data)
        {}
        #endregion
    }
}
