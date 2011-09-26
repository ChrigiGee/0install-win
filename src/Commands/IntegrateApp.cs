﻿/*
 * Copyright 2010-2011 Bastian Eicher
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
using System.Diagnostics.CodeAnalysis;
using Common.Storage;
using Common.Utils;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Injector;
using ZeroInstall.Model;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Add an application to the application list (if missing) and integrate it into the desktop environment.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "C5 types only need to be disposed when using snapshots")]
    [CLSCompliant(false)]
    public sealed class IntegrateApp : AppCommand
    {
        #region Constants
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "integrate-app";
        #endregion

        #region Variables
        /// <summary>A list of all <see cref="AccessPoint"/> categories to be added to the already applied ones.</summary>
        private readonly C5.ICollection<string> _addCategories = new C5.LinkedList<string>();

        /// <summary>A list of all <see cref="AccessPoint"/> categories to be removed from the already applied ones.</summary>
        private readonly C5.ICollection<string> _removeCategories = new C5.LinkedList<string>();
        #endregion

        #region Properties
        /// <inheritdoc/>
        protected override string Description { get { return Resources.DescriptionIntegrateApp; } }
        #endregion

        #region Constructor
        /// <inheritdoc/>
        public IntegrateApp(Policy policy) : base(policy)
        {
            string categoryList = StringUtils.Concatenate(CategoryIntegrationManager.Categories, ", ");

            Options.Add("a|add=", Resources.OptionAppAdd + "\n" + Resources.OptionAppCategory + categoryList, category =>
            {
                category = category.ToLower();
                if (!CategoryIntegrationManager.Categories.Contains(category)) throw new OptionException(string.Format(Resources.UnknownCategory, category), "add");
                _addCategories.Add(category);
            });
            Options.Add("x|remove=", Resources.OptionAppRemove + "\n" + Resources.OptionAppCategory + categoryList, category =>
            {
                category = category.ToLower();
                if (!CategoryIntegrationManager.Categories.Contains(category)) throw new OptionException(string.Format(Resources.UnknownCategory, category), "remove");
                _removeCategories.Add(category);
            });
        }
        #endregion

        //--------------------//

        #region Execute
        /// <inheritdoc/>
        public override int Execute()
        {
            if (Locations.IsPortable) throw new NotSupportedException(Resources.NotAvailableInPortableMode);

            return base.Execute();
        }

        /// <inheritdoc/>
        protected override int ExecuteHelper(CategoryIntegrationManager integrationManager, string interfaceID)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(interfaceID)) throw new ArgumentNullException("interfaceID");
            if (integrationManager == null) throw new ArgumentNullException("integrationManager");
            #endregion

            // If the user only wants to remove stuff avoid fetching the feed
            if (_addCategories.IsEmpty && !_removeCategories.IsEmpty)
                return RemoveOnly(integrationManager, interfaceID);

            var feed = GetFeed(interfaceID);
            var appEntry = GetAppEntry(integrationManager, interfaceID, feed);

            // If user specified no specific integration options show an interactive UI
            if (_addCategories.IsEmpty && _removeCategories.IsEmpty)
            {
                Policy.Handler.ShowIntegrateApp(integrationManager, appEntry, feed);
                return 0;
            }

            return RemoveAndAdd(integrationManager, feed, appEntry);
        }

        /// <summary>
        /// Applies the <see cref="_removeCategories"/> specified by the user.
        /// </summary>
        /// <returns>The exit status code to end the process with. 0 means OK, 1 means generic error.</returns>
        private int RemoveOnly(CategoryIntegrationManager integrationManager, string interfaceID)
        {
            try
            {
                integrationManager.RemoveAccessPointCategories(integrationManager.AppList.GetEntry(interfaceID), _removeCategories);
            }
                #region Error handling
            catch (KeyNotFoundException ex)
            {
                // Show a "failed to comply" message (but not in batch mode, since it is too unimportant)
                if (!Policy.Handler.Batch) Policy.Handler.Output(Resources.AppList, ex.Message);
                return 1;
            }
            #endregion

            // Show a "integration complete" message without application name (but not in batch mode, since it is too unimportant)
            if (!Policy.Handler.Batch) Policy.Handler.Output(Resources.DesktopIntegration, string.Format(Resources.DesktopIntegrationDone, interfaceID));
            return 0;
        }

        /// <summary>
        /// Applies the <see cref="_removeCategories"/> and <see cref="_addCategories"/> specified by the user.
        /// </summary>
        /// <returns>The exit status code to end the process with. 0 means OK, 1 means generic error.</returns>
        private int RemoveAndAdd(CategoryIntegrationManager integrationManager, Feed feed, AppEntry appEntry)
        {
            if (!_removeCategories.IsEmpty)
                integrationManager.RemoveAccessPointCategories(appEntry, _removeCategories);

            if (!_addCategories.IsEmpty)
            {
                try
                {
                    integrationManager.AddAccessPointCategories(appEntry, feed, _addCategories, Policy.Handler);
                }
                    #region Error handling
                catch (InvalidOperationException ex)
                {
                    // Show a "failed to comply" message (but not in batch mode, since it is too unimportant)
                    if (!Policy.Handler.Batch) Policy.Handler.Output(Resources.AppList, ex.Message);
                    return 1;
                }
                #endregion
            }

            // Show a "integration complete" message without application name (but not in batch mode, since it is too unimportant)
            if (!Policy.Handler.Batch) Policy.Handler.Output(Resources.DesktopIntegration, string.Format(Resources.DesktopIntegrationDone, appEntry.Name));
            return 0;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Finds an existing <see cref="AppEntry"/> or creates a new one for a specific interface ID and feed.
        /// </summary>
        private AppEntry GetAppEntry(CategoryIntegrationManager integrationManager, string interfaceID, Feed feed)
        {
            AppEntry appEntry;
            try
            {
                appEntry = integrationManager.AppList.GetEntry(interfaceID);

                if (!appEntry.CapabilityLists.UnsequencedEquals(feed.CapabilityLists))
                {
                    string changedMessage = string.Format(Resources.CapabilitiesChanged, appEntry.Name);
                    if (Policy.Handler.AskQuestion(changedMessage + " " + Resources.AskUpdateCapabilities, changedMessage))
                        ((IIntegrationManager)integrationManager).UpdateApp(appEntry, feed);
                }
            }
            catch (KeyNotFoundException)
            {
                // Automatically add missing AppEntry
                appEntry = integrationManager.AddApp(interfaceID, feed);
            }
            return appEntry;
        }
        #endregion
    }
}
