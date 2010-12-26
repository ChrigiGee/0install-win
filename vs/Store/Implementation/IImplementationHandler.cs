﻿/*
 * Copyright 2010 Bastian Eicher
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

namespace ZeroInstall.Store.Implementation
{
    /// <summary>
    /// Callback methods to be used when the the user is to be informed about extraction and manifest progress.
    /// </summary>
    /// <remarks>The callbacks may be called from a background thread. Apply thread-synchronization to update UI elements.</remarks>
    public interface IImplementationHandler
    {
        /// <summary>
        /// Don't print messages to <see cref="Console"/> unless errors occur and don't block with questions or messages.
        /// </summary>
        bool Batch { get; set; }

        /// <summary>
        /// Called when a new extraction task is about to be started.
        /// </summary>
        /// <param name="extraction">A reference to the extraction task. Can be used for tracking the progress.</param>
        /// <remarks>The <see cref="IProgress.State"/> may still be <see cref="ProgressState.Ready"/> and not <see cref="ProgressState.Started"/> yet.</remarks>
        void StartingExtraction(IProgress extraction);

        /// <summary>
        /// Called when a new manifest is about to be generated.
        /// </summary>
        /// <param name="manifest">A reference to the manifest generation task. Can be used for tracking the progress.</param>
        /// <remarks>The <see cref="IProgress.State"/> may still be <see cref="ProgressState.Ready"/> and not <see cref="ProgressState.Started"/> yet.</remarks>
        void StartingManifest(IProgress manifest);
    }
}
