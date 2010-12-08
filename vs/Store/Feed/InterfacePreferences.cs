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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using Common.Storage;
using ZeroInstall.Model;

namespace ZeroInstall.Store.Feed
{
    /// <summary>
    /// Stores user-specific preferences for an interface.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "C5 collections don't need to be disposed.")]
    [XmlRoot("interface-preferences", Namespace = Model.Feed.XmlNamespace)]
    [XmlType("interface-preferences", Namespace = Model.Feed.XmlNamespace)]
    public sealed class InterfacePreferences : XmlUnknown
    {
        #region Properties
        /// <summary>
        /// The URI of the interface to be configured.
        /// </summary>
        [Description("The URI of the interface to be configured.")]
        [XmlIgnore]
        public Uri Uri { get; set; }

        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="Uri"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [XmlAttribute("uri"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public String UriString
        {
            get { return (Uri == null ? null : Uri.ToString()); }
            set { Uri = (value == null ? null : new Uri(value)); }
        }

        private Stability _stabilityPolicy = Stability.Unset;
        /// <summary>
        /// Implementations at this stability level or higher are preferred. Lower levels are used only if there is no other choice.
        /// </summary>
        [Description("Implementations at this stability level or higher are preferred. Lower levels are used only if there is no other choice.")]
        [XmlAttribute("stability-policy"), DefaultValue(typeof(Stability), "Unset")]
        public Stability StabilityPolicy { get { return _stabilityPolicy; } set { _stabilityPolicy = value; } }

        // Preserve order
        private readonly C5.ArrayList<FeedReference> _feeds = new C5.ArrayList<FeedReference>();
        /// <summary>
        /// Zero ore more additional feeds containing implementations of this interface.
        /// </summary>
        [Description("Zero ore more additional feeds containing implementations of this interface.")]
        [XmlElement("feed")]
        // Note: Can not use ICollection<T> interface with XML Serialization
        public C5.ArrayList<FeedReference> Feeds { get { return _feeds; } }
        #endregion

        //--------------------//

        #region Storage
        /// <summary>
        /// Loads <see cref="InterfacePreferences"/> from an XML file (feed).
        /// </summary>
        /// <param name="path">The file to load from.</param>
        /// <returns>The loaded <see cref="InterfacePreferences"/>.</returns>
        /// <exception cref="IOException">Thrown if a problem occurs while reading the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if read access to the file is not permitted.</exception>
        /// <exception cref="InvalidOperationException">Thrown if a problem occurs while deserializing the XML data.</exception>
        public static InterfacePreferences Load(string path)
        {
            return XmlStorage.Load<InterfacePreferences>(path);
        }

        /// <summary>
        /// Loads <see cref="InterfacePreferences"/> from a stream containing an XML file (feed).
        /// </summary>
        /// <param name="stream">The stream to load from.</param>
        /// <returns>The loaded <see cref="InterfacePreferences"/>.</returns>
        public static InterfacePreferences Load(Stream stream)
        {
            return XmlStorage.Load<InterfacePreferences>(stream);
        }

        /// <summary>
        /// Saves this <see cref="InterfacePreferences"/> to an XML file (feed).
        /// </summary>
        /// <param name="path">The file to save in.</param>
        /// <exception cref="IOException">Thrown if a problem occurs while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if write access to the file is not permitted.</exception>
        public void Save(string path)
        {
            XmlStorage.Save(path, this);
        }

        /// <summary>
        /// Saves this <see cref="InterfacePreferences"/> to a stream as an XML file (feed).
        /// </summary>
        /// <param name="stream">The stream to save in.</param>
        public void Save(Stream stream)
        {
            XmlStorage.Save(stream, this);
        }
        #endregion

        //--------------------//

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="InterfacePreferences"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="InterfacePreferences"/>.</returns>
        public InterfacePreferences CloneInterfacePreferences()
        {
            var feed = new InterfacePreferences {Uri = Uri, StabilityPolicy = StabilityPolicy};
            foreach (var feedReference in Feeds) feed.Feeds.Add(feedReference.CloneFeedPreferences());

            return feed;
        }

        public object Clone()
        {
            return CloneInterfacePreferences();
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the preferences in the form "InterfacePreferences: Uri". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return string.Format("InterfacePreferences: {0}", Uri);
        }
        #endregion

        #region Equality
        public bool Equals(InterfacePreferences other)
        {
            if (ReferenceEquals(null, other)) return false;

            if (Uri != other.Uri) return false;
            if (StabilityPolicy != other.StabilityPolicy) return false;
            if (!Feeds.SequencedEquals(other.Feeds)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(InterfacePreferences) && Equals((InterfacePreferences)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (UriString ?? "").GetHashCode();
                result = (result * 397) ^ StabilityPolicy.GetHashCode();
                result = (result * 397) ^ Feeds.GetSequencedHashCode();
                return result;
            }
        }
        #endregion
    }
}
