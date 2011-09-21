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
using Common;
using Common.Collections;
using Common.Storage;
using Common.Utils;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.DesktopIntegration;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.Injector;

namespace ZeroInstall.Commands
{
    /// <summary>
    /// Create an alias for a <see cref="Run"/> command.
    /// </summary>
    [CLSCompliant(false)]
    public sealed class AddAlias : IntegrationCommand
    {
        #region Constants
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "add-alias";
        #endregion

        #region Variables
        private bool _resolve;
        private bool _remove;
        #endregion

        #region Properties
        /// <inheritdoc/>
        protected override string Usage { get { return "ALIAS [INTERFACE [COMMAND]]"; } }

        /// <inheritdoc/>
        protected override string Description { get { return Resources.DescriptionAddAlias; } }

        /// <inheritdoc/>
        public override string ActionTitle { get { return Resources.ActionAppCommand; } }
        #endregion

        #region Constructor
        /// <inheritdoc/>
        public AddAlias(Policy policy) : base(policy)
        {
            Options.Add("resolve", Resources.OptionAliasResolve, unused => _resolve = true);
            Options.Add("remove", Resources.OptionAliasRemove, unused => _remove = true);
        }
        #endregion

        //--------------------//

        #region Execute
        /// <inheritdoc/>
        public override int Execute()
        {
            #region Sanity checks
            if (!IsParsed) throw new InvalidOperationException(Resources.NotParsed);
            #endregion

            if (Locations.IsPortable) throw new NotSupportedException(Resources.NotAvailableInPortableMode);
            if (SystemWide && WindowsUtils.IsWindows && !WindowsUtils.IsAdministrator) return RerunAsAdmin();

            if (AdditionalArgs.Count < 1 || string.IsNullOrEmpty(AdditionalArgs[0])) throw new OptionException(Resources.MissingArguments, "");

            using (var integrationManager = new IntegrationManager(SystemWide, Policy.Handler))
            {
                if (_resolve || _remove)
                {
                    if (AdditionalArgs.Count > 1) throw new OptionException(Resources.TooManyArguments, "");

                    return ResolveOrRemove(integrationManager, AdditionalArgs[0]);
                }

                if (AdditionalArgs.Count < 2 || string.IsNullOrEmpty(AdditionalArgs[1])) throw new OptionException(Resources.MissingArguments, "");
                if (AdditionalArgs.Count > 3) throw new OptionException(Resources.TooManyArguments, "");

                string interfaceID = GetCanonicalID(AdditionalArgs[1]);
                string command = (AdditionalArgs.Count >= 3 ? AdditionalArgs[2] : null);
                return CreateAlias(integrationManager, AdditionalArgs[0], interfaceID, command);
            }
        }

        /// <summary>
        /// Creates a new alias.
        /// </summary>
        /// <param name="integrationManager">Manages desktop integration operations.</param>
        /// <param name="aliasName">The name of the alias to create.</param>
        /// <param name="interfaceID">The interface ID the alias shall point to.</param>
        /// <param name="command">A command within the interface the alias shall point to; may be <see langword="null"/>.</param>
        /// <returns>The exit status code to end the process with. 0 means OK, 1 means generic error.</returns>
        private int CreateAlias(IIntegrationManager integrationManager, string aliasName, string interfaceID, string command)
        {
            Policy.Handler.ShowProgressUI(Cancel);
            bool stale;
            var feed = Policy.FeedManager.GetFeed(interfaceID, Policy, out stale);

            if (Canceled) throw new UserCancelException();

            // Apply the new alias
            var alias = new AppAlias {Name = aliasName, Command = command};
            try { integrationManager.AddAccessPoints(new InterfaceFeed(interfaceID, feed), new AccessPoint[] {alias}); }
            catch (InvalidOperationException ex)
            {
                // Show a "failed to comply" message
                Policy.Handler.Output(Resources.AppAlias, ex.Message);
                return 1;
            }

            // Show a "integration complete" message (but not in batch mode, since it is too unimportant)
            if (!Policy.Handler.Batch) Policy.Handler.Output(Resources.DesktopIntegration, string.Format(Resources.AliasCreated, aliasName, feed.Name));
            return 0;
        }

        /// <summary>
        /// Resolves or removes existing aliases.
        /// </summary>
        /// <param name="integrationManager">Manages desktop integration operations.</param>
        /// <param name="aliasName">The name of the existing alias.</param>
        /// <returns>The exit status code to end the process with. 0 means OK, 1 means generic error.</returns>
        private int ResolveOrRemove(IIntegrationManager integrationManager, string aliasName)
        {
            AppEntry appEntry;
            var appAlias = GetAppAlias(integrationManager.AppList, aliasName, out appEntry);
            if (appAlias == null)
            {
                Policy.Handler.Output(Resources.AppAlias, string.Format(Resources.AliasNotFound, aliasName));
                return 1;
            }

            if (_resolve)
            {
                string result = appEntry.InterfaceID;
                if (!string.IsNullOrEmpty(appAlias.Command)) result += Environment.NewLine + "Command: " + appAlias.Command;
                Policy.Handler.Output(Resources.AppAlias, result);
            }
            if (_remove)
            {
                integrationManager.RemoveAccessPoints(appEntry.InterfaceID, new AccessPoint[] {appAlias});

                // Show a "integration complete" message (but not in batch mode, since it is too unimportant)
                Policy.Handler.Output(Resources.AppAlias, string.Format(Resources.AliasRemoved, aliasName, appEntry.Name));
            }
            return 0;
        }

        /// <summary>
        /// Retreives a specific <see cref="AppAlias"/>.
        /// </summary>
        /// <param name="appList">The list of <see cref="AppEntry"/>s to search.</param>
        /// <param name="aliasName">The name of the alias to search for.</param>
        /// <param name="foundAppEntry">Returns the <see cref="AppEntry"/> containing the found <see cref="AppAlias"/>; <see langword="null"/> if none was found.</param>
        /// <returns>The first <see cref="AppAlias"/> in <paramref name="appList"/> matching <paramref name="aliasName"/>; <see langword="null"/> if none was found.</returns>
        private static AppAlias GetAppAlias(AppList appList, string aliasName, out AppEntry foundAppEntry)
        {
            foreach (var appEntry in appList.Entries)
            {
                if (appEntry.AccessPoints == null) continue;
                foreach (var appAlias in EnumerableUtils.OfType<AppAlias>(appEntry.AccessPoints.Entries))
                {
                    if (appAlias.Name == aliasName)
                    {
                        foundAppEntry = appEntry;
                        return appAlias;
                    }
                }
            }

            foundAppEntry = null;
            return null;
        }
        #endregion
    }
}