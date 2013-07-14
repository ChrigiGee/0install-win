﻿/*
 * Copyright 2010-2013 Bastian Eicher
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
using System.IO;
using System.Linq;
using System.Net;
using Common;
using Common.Collections;
using Common.Storage;
using Common.Tasks;
using Common.Undo;
using Common.Utils;
using ZeroInstall.Model;
using ZeroInstall.Publish.Properties;
using ZeroInstall.Store.Implementation;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// Helper methods for manipulating <see cref="Implementation"/>s.
    /// </summary>
    public static class ImplementationUtils
    {
        #region Constants
        private const string Sha1Empty = "da39a3ee5e6b4b0d3255bfef95601890afd80709";
        #endregion

        #region Build
        /// <summary>
        /// Creates a new <see cref="Implementation"/> by completing a <see cref="RetrievalMethod"/> and calculating the resulting <see cref="ManifestDigest"/>.
        /// </summary>
        /// <param name="retrievalMethod">The <see cref="RetrievalMethod"/> to use.</param>
        /// <param name="store">Adds the downloaded archive to the default <see cref="IStore"/> when set to <see langword="true"/>.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns>A newly created <see cref="Implementation"/> containing one <see cref="Archive"/>.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the user canceled the task.</exception>
        /// <exception cref="IOException">Thrown if there was a problem extracting the archive.</exception>
        /// <exception cref="WebException">Thrown if there was a problem downloading the archive.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if write access to temporary files was not permitted.</exception>
        /// <exception cref="NotSupportedException">Thrown if the archive's MIME type could not be determined.</exception>
        public static Implementation Build(RetrievalMethod retrievalMethod, bool store, ITaskHandler handler)
        {
            #region Sanity checks
            if (retrievalMethod == null) throw new ArgumentNullException("retrievalMethod");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            TemporaryDirectory implementationDir = null;
            new PerTypeDispatcher<RetrievalMethod>(false)
            {
                (Archive archive) => implementationDir = DownloadArchive(archive, handler),
                (SingleFile file) => implementationDir = DownloadSingleFile(file, handler),
                (Recipe recipe) => implementationDir = DownloadRecipe(recipe, handler)
            }.Dispatch(retrievalMethod);

            try
            {
                var digest = GenerateDigest(implementationDir, store, handler);
                return new Implementation {ID = "sha1new=" + digest.Sha1New, ManifestDigest = digest, RetrievalMethods = {retrievalMethod}};
            }
            finally
            {
                implementationDir.Dispose();
            }
        }
        #endregion

        #region Add missing
        /// <summary>
        /// Adds missing data (by downloading and infering) to an <see cref="Implementation"/>.
        /// </summary>
        /// <param name="implementation">The <see cref="Implementation"/> to add data to.</param>
        /// <param name="store"><see langword="true"/> to store the directory as an implementation in the default <see cref="IStore"/>.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion; may be <see langword="null"/>.</param>
        public static void AddMissing(Implementation implementation, bool store, ITaskHandler handler, ICommandExecutor executor = null)
        {
            #region Sanity checks
            if (implementation == null) throw new ArgumentNullException("implementation");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            ConvertSha256ToSha256New(implementation, executor);

            var downloadDispatcher = new PerTypeDispatcher<RetrievalMethod, TemporaryDirectory>(true)
            {
                (Archive archive) => DownloadArchive(archive, handler, executor),
                (SingleFile file) => DownloadSingleFile(file, handler, executor),
                (Recipe recipe) => DownloadRecipe(recipe, handler, executor)
            };

            foreach (var retrievalMethod in implementation.RetrievalMethods)
            {
                if (implementation.ManifestDigest == default(ManifestDigest) || IsDownloadSizeMissing(retrievalMethod))
                {
                    using (var tempDir = downloadDispatcher.Dispatch(retrievalMethod))
                        UpdateDigest(implementation, tempDir, store, handler, executor);
                }
            }

            if (string.IsNullOrEmpty(implementation.ID)) implementation.ID = "sha1new=" + implementation.ManifestDigest.Sha1New;
        }

        private static void ConvertSha256ToSha256New(Implementation implementation, ICommandExecutor executor = null)
        {
            if (string.IsNullOrEmpty(implementation.ManifestDigest.Sha256) || !string.IsNullOrEmpty(implementation.ManifestDigest.Sha256New)) return;

            var digest = new ManifestDigest(
                implementation.ManifestDigest.Sha1,
                implementation.ManifestDigest.Sha1New,
                implementation.ManifestDigest.Sha256,
                implementation.ManifestDigest.Sha256.Base16Decode().Base32Encode());

            if (executor == null) implementation.ManifestDigest = digest;
            else executor.Execute(new SetValueCommand<ManifestDigest>(() => implementation.ManifestDigest, value => implementation.ManifestDigest = value, digest));
        }

        private static bool IsDownloadSizeMissing(RetrievalMethod retrievalMethod)
        {
            var downloadRetrievalMethod = retrievalMethod as DownloadRetrievalMethod;
            return downloadRetrievalMethod != null && downloadRetrievalMethod.Size == 0;
        }
        #endregion

        #region Download
        /// <summary>
        /// Downloads and extracts an <see cref="Archive"/> and adds missing properties.
        /// </summary>
        /// <param name="archive">The <see cref="Archive"/> to be downloaded.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion; may be <see langword="null"/>.</param>
        /// <returns>A temporary directory containing the contents of the archive.</returns>
        public static TemporaryDirectory DownloadArchive(Archive archive, ITaskHandler handler, ICommandExecutor executor = null)
        {
            #region Sanity checks
            if (archive == null) throw new ArgumentNullException("archive");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            using (var downloadedFile = Download(archive, handler, executor))
            {
                var extractionDir = new TemporaryDirectory("0publish");
                try
                {
                    RecipeUtils.ApplyArchive(archive, downloadedFile, extractionDir, handler, null);
                    return extractionDir;
                }
                    #region Error handling
                catch
                {
                    extractionDir.Dispose();
                    throw;
                }
                #endregion
            }
        }

        /// <summary>
        /// Downloads a <see cref="SingleFile"/> and adds missing properties.
        /// </summary>
        /// <param name="file">The <see cref="SingleFile"/> to be downloaded.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion; may be <see langword="null"/>.</param>
        /// <returns>A temporary directory containing the file.</returns>
        public static TemporaryDirectory DownloadSingleFile(SingleFile file, ITaskHandler handler, ICommandExecutor executor = null)
        {
            #region Sanity checks
            if (file == null) throw new ArgumentNullException("file");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            using (var downloadedFile = Download(file, handler, executor))
            {
                var extractionDir = new TemporaryDirectory("0publish");
                try
                {
                    RecipeUtils.ApplySingleFile(file, downloadedFile, extractionDir);
                    return extractionDir;
                }
                    #region Error handling
                catch
                {
                    extractionDir.Dispose();
                    throw;
                }
                #endregion
            }
        }

        /// <summary>
        /// Downloads a <see cref="DownloadRetrievalMethod"/> and adds missing properties.
        /// </summary>
        /// <param name="retrievalMethod">The <see cref="DownloadRetrievalMethod"/> to be downloaded.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion; may be <see langword="null"/>.</param>
        /// <returns>A downloaded file.</returns>
        private static TemporaryFile Download(DownloadRetrievalMethod retrievalMethod, ITaskHandler handler, ICommandExecutor executor)
        {
            #region Sanity checks
            if (retrievalMethod == null) throw new ArgumentNullException("retrievalMethod");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            // Ensure things like MIME types are not lost
            retrievalMethod.Normalize();

            // Download the file
            var downloadedFile = new TemporaryFile("0publish");
            handler.RunTask(new DownloadFile(retrievalMethod.Href, downloadedFile)); // Defer task to handler

            // Set downloaded file size
            long newSize = new FileInfo(downloadedFile).Length;
            if (retrievalMethod.Size != newSize)
            {
                if (executor == null) retrievalMethod.Size = newSize;
                else executor.Execute(new SetValueCommand<long>(() => retrievalMethod.Size, value => retrievalMethod.Size = value, newSize));
            }

            return downloadedFile;
        }

        /// <summary>
        /// Downloads and applies a <see cref="Recipe"/> and adds missing properties.
        /// </summary>
        /// <param name="recipe">The <see cref="Recipe"/> to be applied.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion; may be <see langword="null"/>.</param>
        /// <returns>A temporary directory containing the result of the recipe.</returns>
        public static TemporaryDirectory DownloadRecipe(Recipe recipe, ITaskHandler handler, ICommandExecutor executor = null)
        {
            #region Sanity checks
            if (recipe == null) throw new ArgumentNullException("recipe");
            if (handler == null) throw new ArgumentNullException("handler");
            #endregion

            var downloadedFiles = new List<TemporaryFile>();
            try
            {
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (var step in recipe.Steps.OfType<DownloadRetrievalMethod>())
                    downloadedFiles.Add(Download(step, handler, executor));
                // ReSharper restore LoopCanBeConvertedToQuery

                // Apply the recipe
                return RecipeUtils.ApplyRecipe(recipe, downloadedFiles, handler, null);
            }
            finally
            {
                // Clean up temporary archive files
                foreach (var downloadedFile in downloadedFiles) downloadedFile.Dispose();
            }
        }
        #endregion

        #region Digest helpers
        /// <summary>
        /// Updates the <see cref="ManifestDigest"/> in an <see cref="Implementation"/>.
        /// </summary>
        /// <param name="implementation">The <see cref="Implementation"/> to update.</param>
        /// <param name="path">The path of the directory to generate the digest for.</param>
        /// <param name="store"><see langword="true"/> to store the directory as an implementation in the default <see cref="IStore"/>.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <param name="executor">Used to apply properties in an undoable fashion; may be <see langword="null"/>.</param>
        private static void UpdateDigest(Implementation implementation, string path, bool store, ITaskHandler handler, ICommandExecutor executor = null)
        {
            var digest = GenerateDigest(path, store, handler);

            if (implementation.ManifestDigest == default(ManifestDigest))
            {
                if (executor == null) implementation.ManifestDigest = digest;
                else executor.Execute(new SetValueCommand<ManifestDigest>(() => implementation.ManifestDigest, value => implementation.ManifestDigest = value, digest));
            }
            if (digest != implementation.ManifestDigest)
                throw new DigestMismatchException(implementation.ManifestDigest.ToString(), null, digest.ToString(), null);
        }

        /// <summary>
        /// Generates the <see cref="ManifestDigest"/> for a directory.
        /// </summary>
        /// <param name="path">The path of the directory to generate the digest for.</param>
        /// <param name="store"><see langword="true"/> to store the directory as an implementation in the default <see cref="IStore"/>.</param>
        /// <param name="handler">A callback object used when the the user is to be informed about progress.</param>
        /// <returns>The newly generated digest.</returns>
        public static ManifestDigest GenerateDigest(string path, bool store, ITaskHandler handler)
        {
            var digest = Manifest.CreateDigest(path, handler);
            if (store)
            {
                try
                {
                    StoreFactory.CreateDefault().AddDirectory(path, digest, handler);
                }
                catch (ImplementationAlreadyInStoreException)
                {}
            }

            if (digest.Sha1New == Sha1Empty) Log.Warn(string.Format(Resources.EmptyImplementation, path));
            return digest;
        }
        #endregion
    }
}
