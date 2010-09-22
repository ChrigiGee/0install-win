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
using System.Xml.Serialization;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Make a chosen <see cref="Implementation"/> available by overlaying it onto another part of the file-system.
    /// </summary>
    /// <remarks>This is to support legacy programs which use hard-coded paths.</remarks>
    public sealed class OverlayBinding : Binding, IEquatable<OverlayBinding>
    {
        #region Properties
        private string _source = ".";
        /// <summary>
        /// The relative path of the directory in the implementation to publish. The default is to publish everything.
        /// </summary>
        [Description("The name of the environment variable.")]
        [XmlAttribute("src")]
        public string Source { get { return _source; } set { _source = value; } }

        /// <summary>
        /// The mount point on which src is to appear in the filesystem. If missing, '/' (on POSIX) or '%systemdrive%' (on Windows) is assumed.
        /// </summary>
        [Description("The mount point on which src is to appear in the filesystem. If missing, '/' (on POSIX) or '%systemdrive%' (on Windows) is assumed.")]
        [XmlAttribute("mount-point")]
        public string MountPoint { get; set; }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns the binding in the form "OverlayBinding: Source => MountPoint". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return string.Format("OverlayBinding: {0} => {1}", Source, MountPoint);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="OverlayBinding"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="OverlayBinding"/>.</returns>
        public override Binding CloneBinding()
        {
            return new OverlayBinding { Source = Source, MountPoint = MountPoint };
        }
        #endregion

        #region Equality
        public bool Equals(OverlayBinding other)
        {
            if (ReferenceEquals(null, other)) return false;

            return other.Source == Source || other.MountPoint == MountPoint;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(OverlayBinding) && Equals((OverlayBinding)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source != null ? Source.GetHashCode() : 0) * 397) ^ (MountPoint != null ? MountPoint.GetHashCode() : 0);
            }
        }
        #endregion
    }
}
