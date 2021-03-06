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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// Provides access to a disk-based cache of <see cref="Feed"/>s that were downloaded via HTTP(S).
    /// </summary>
    /// <remarks>
    ///   <para>Local feed files are simply passed through this cache.</para>
    ///   <para>Once a feed has been added to this cache it is considered trusted (signatures are not checked again).</para>
    /// </remarks>
    public sealed class DiskFeedCache : IFeedCache
    {
        #region Dependencies
        private readonly IOpenPgp _openPgp;

        /// <summary>
        /// Creates a new disk-based cache.
        /// </summary>
        /// <param name="path">A fully qualified directory path.</param>
        /// <param name="openPgp">Provides access to an encryption/signature system compatible with the OpenPGP standard.</param>
        public DiskFeedCache([NotNull] string path, [NotNull] IOpenPgp openPgp)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            if (openPgp == null) throw new ArgumentNullException("openPgp");
            #endregion

            DirectoryPath = path;
            _openPgp = openPgp;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The directory containing the cached <see cref="Feed"/>s.
        /// </summary>
        public string DirectoryPath { get; private set; }
        #endregion

        //--------------------//

        #region Contains
        /// <inheritdoc/>
        public bool Contains(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException("feedUri");
            #endregion

            // Local files are passed through directly
            if (feedUri.IsFile) return File.Exists(feedUri.LocalPath);

            return FileUtils.ExistsCaseSensitive(Path.Combine(DirectoryPath, feedUri.Escape()));
        }
        #endregion

        #region List all
        /// <inheritdoc/>
        public IEnumerable<FeedUri> ListAll()
        {
            if (!Directory.Exists(DirectoryPath)) return Enumerable.Empty<FeedUri>();

            // ReSharper disable once AssignNullToNotNullAttribute
            return Directory.GetFiles(DirectoryPath)
                .TrySelect<string, FeedUri, UriFormatException>(x => FeedUri.Unescape(Path.GetFileName(x)));
        }
        #endregion

        #region Get
        /// <inheritdoc/>
        public Feed GetFeed(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException("feedUri");
            #endregion

            string path = GetPath(feedUri);
            Log.Debug("Loading feed " + feedUri.ToStringRfc() + " from disk cache: " + path);

            var feed = XmlStorage.LoadXml<Feed>(path);
            feed.Normalize(feedUri);
            return feed;
        }

        /// <inheritdoc/>
        public IEnumerable<OpenPgpSignature> GetSignatures(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException("feedUri");
            #endregion

            return FeedUtils.GetSignatures(_openPgp, File.ReadAllBytes(GetPath(feedUri)));
        }

        /// <summary>
        /// Determines the file path used to store a feed with a particular ID.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The requested <paramref name="feedUri"/> was not found in the cache.</exception>
        private string GetPath(FeedUri feedUri)
        {
            if (feedUri.IsFile) return feedUri.LocalPath;
            {
                string fileName = feedUri.Escape();
                string path = Path.Combine(DirectoryPath, fileName);
                if (FileUtils.ExistsCaseSensitive(path)) return path;
                else throw new KeyNotFoundException(string.Format(Resources.FeedNotInCache, feedUri, path));
            }
        }
        #endregion

        #region Add
        /// <inheritdoc/>
        public void Add(FeedUri feedUri, byte[] data)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException("feedUri");
            if (data == null) throw new ArgumentNullException("data");
            #endregion

            if (!Directory.Exists(DirectoryPath)) Directory.CreateDirectory(DirectoryPath);

            try
            {
                string path = Path.Combine(DirectoryPath, feedUri.Escape());
                Log.Debug("Adding feed " + feedUri.ToStringRfc() + " to disk cache: " + path);
                WriteToFile(data, path);
            }
            catch (PathTooLongException)
            {
                Log.Info("File path in feed cache too long. Using hash of feed URI to shorten path.");
                WriteToFile(data, Path.Combine(DirectoryPath, feedUri.AbsoluteUri.Hash(SHA256.Create())));
            }
        }

        private readonly object _replaceLock = new object();

        /// <summary>
        /// Writes the entire content of a byte array to file atomically.
        /// </summary>
        /// <param name="data">The data to write.</param>
        /// <param name="path">The file to write to.</param>
        private void WriteToFile(byte[] data, string path)
        {
            lock (_replaceLock)
                using (var atomic = new AtomicWrite(path))
                {
                    File.WriteAllBytes(atomic.WritePath, data);
                    atomic.Commit();
                }
        }
        #endregion

        #region Remove
        /// <inheritdoc/>
        public void Remove(FeedUri feedUri)
        {
            #region Sanity checks
            if (feedUri == null) throw new ArgumentNullException("feedUri");
            #endregion

            string path = GetPath(feedUri);
            Log.Debug("Removing feed " + feedUri.ToStringRfc() + " from disk cache: " + path);
            File.Delete(path);
        }
        #endregion

        #region Flush
        // No in-memory cache
        void IFeedCache.Flush()
        {}
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns <see cref="DirectoryPath"/>. Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return "DiskFeedCache: " + DirectoryPath;
        }
        #endregion
    }
}
