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
using System.IO;
using JetBrains.Annotations;
using NanoByte.Common.Storage;
using NanoByte.Common.Streams;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Publish
{
    /// <summary>
    /// A wrapper around a <see cref="Feed"/> adding and XSL stylesheet and a digital signature.
    /// </summary>
    [Serializable]
    public class SignedFeed
    {
        /// <summary>
        /// The wrapped <see cref="Feed"/>.
        /// </summary>
        [NotNull]
        public Feed Feed { get; set; }

        /// <summary>
        /// The secret key used to sign the <see cref="Feed"/>; <see langword="null"/> for no signature.
        /// </summary>
        [CanBeNull]
        public OpenPgpSecretKey SecretKey { get; set; }

        /// <summary>
        /// Creates a new signed feed.
        /// </summary>
        /// <param name="feed">The wrapped <see cref="Feed"/>.</param>
        /// <param name="secretKey">The secret key used to sign the <see cref="Feed"/>; <see langword="null"/> for no signature.</param>
        public SignedFeed([NotNull] Feed feed, [CanBeNull] OpenPgpSecretKey secretKey = null)
        {
            #region Sanity checks
            if (feed == null) throw new ArgumentNullException("feed");
            #endregion

            Feed = feed;
            SecretKey = secretKey;
        }

        #region Storage
        /// <summary>
        /// Loads a <see cref="Feed"/> from an XML file and identifies the signature (if any).
        /// </summary>
        /// <param name="path">The file to load from.</param>
        /// <returns>The loaded <see cref="SignedFeed"/>.</returns>
        /// <exception cref="IOException">A problem occurs while reading the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Read access to the file is not permitted.</exception>
        /// <exception cref="InvalidDataException">A problem occurs while deserializing the XML data.</exception>
        [NotNull]
        public static SignedFeed Load([NotNull] string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            #endregion

            return new SignedFeed(XmlStorage.LoadXml<Feed>(path), FeedUtils.GetKey(path, OpenPgpFactory.CreateDefault()));
        }

        /// <summary>
        /// Saves <see cref="Feed"/> to an XML file, adds the default stylesheet and sign it it with <see cref="SecretKey"/> (if specified).
        /// </summary>
        /// <remarks>Writing and signing the feed file are performed as an atomic operation (i.e. if signing fails an existing file remains unchanged).</remarks>
        /// <param name="path">The file to save in.</param>
        /// <param name="passphrase">The passphrase to use to unlock the secret key; can be <see langword="null"/> if <see cref="SecretKey"/> is <see langword="null"/>.</param>
        /// <exception cref="IOException">A problem occurs while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
        /// <exception cref="WrongPassphraseException">Passphrase was incorrect.</exception>
        public void Save([NotNull] string path, [CanBeNull] string passphrase = null)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            #endregion

            if (SecretKey == null)
            {
                Feed.SaveXml(path);
                return;
            }

            var openPgp = OpenPgpFactory.CreateDefault();
            using (var stream = new MemoryStream())
            {
                Feed.SaveXml(stream, stylesheet: @"feed.xsl");
                stream.Position = 0;

                FeedUtils.SignFeed(stream, SecretKey, passphrase, openPgp);
                stream.WriteTo(path);
            }
            string directory = Path.GetDirectoryName(path);
            if (directory != null)
            {
                FeedUtils.DeployPublicKey(directory, SecretKey, openPgp);
                FeedUtils.DeployStylesheet(directory, @"feed");
            }
        }
        #endregion
    }
}
