/*
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
using System.Diagnostics;
using JetBrains.Annotations;
using NanoByte.Common.Tasks;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Fetchers;
using ZeroInstall.Services.Injector;
using ZeroInstall.Services.Properties;
using ZeroInstall.Services.Solvers.ExternalJson;
using ZeroInstall.Store;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// Uses an external process controlled via a JSON API to solve requirements.
    /// </summary>
    /// <remarks>This class is immutable and thread-safe.</remarks>
    public class ExternalJsonSolver : ISolver
    {
        #region Dependencies
        private readonly ISolver _backingSolver;
        private readonly SelectionsManager _selectionsManager;
        private readonly IFetcher _fetcher;
        private readonly IExecutor _executor;
        private readonly Config _config;
        private readonly IFeedManager _feedManager;
        private readonly ITaskHandler _handler;

        /// <summary>
        /// Creates a new external JSON solver.
        /// </summary>
        /// <param name="backingSolver">An internal solver used to find an implementation of the external solver.</param>
        /// <param name="selectionsManager">Used to check whether the external solver is already in the cache.</param>
        /// <param name="fetcher">Used to download implementations of the external solver.</param>
        /// <param name="executor">Used to launch the external solver.</param>
        /// <param name="config">User settings controlling network behaviour, solving, etc.</param>
        /// <param name="feedManager">Provides access to remote and local <see cref="Feed"/>s. Handles downloading, signature verification and caching.</param>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public ExternalJsonSolver([NotNull] ISolver backingSolver, [NotNull] SelectionsManager selectionsManager, [NotNull] IFetcher fetcher, [NotNull] IExecutor executor, [NotNull] Config config, [NotNull] IFeedManager feedManager, [NotNull] ITaskHandler handler)
        {
            #region Sanity checks
            if (backingSolver == null) throw new ArgumentNullException("backingSolver");
            if (selectionsManager == null) throw new ArgumentNullException("selectionsManager");
            if (fetcher == null) throw new ArgumentNullException("fetcher");
            if (executor == null) throw new ArgumentNullException("executor");
            if (config == null) throw new ArgumentNullException("config");
            if (feedManager == null) throw new ArgumentNullException("feedManager");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            _backingSolver = backingSolver;
            _selectionsManager = selectionsManager;
            _fetcher = fetcher;
            _executor = executor;
            _config = config;
            _feedManager = feedManager;
            _handler = handler;

            _solverRequirements = new Requirements(config.ExternalSolverUri);
        }
        #endregion

        private readonly Requirements _solverRequirements;
        private Selections _solverSelections;

        private Process RunExternalSolver([NotNull] params string[] arguments)
        {
            if (_solverSelections == null)
                _solverSelections = _backingSolver.Solve(_solverRequirements);

            var control = new SolverControl(_solverSelections, _selectionsManager, _fetcher, _executor);
        }

        /// <inheritdoc/>
        public Selections Solve(Requirements requirements)
        {
            #region Sanity checks
            if (requirements == null) throw new ArgumentNullException("requirements");
            if (requirements.InterfaceUri == null) throw new ArgumentException(Resources.MissingInterfaceUri, "requirements");
            #endregion

            // TODO
            throw new NotImplementedException();
        }
    }
}
