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
using System.ComponentModel;
using System.Xml.Serialization;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// Indicates that an application should be listed in Windows Vista/7's "Set your Default Programs" UI.
    /// </summary>
    /// <remarks>The actual integration information is pulled from the other <see cref="Capability"/>s.</remarks>
    [XmlType("registration", Namespace = XmlNamespace)]
    public sealed class AppRegistration : Capability, IEquatable<AppRegistration>
    {
        #region Properties
        /// <inheritdoc/>
        public override bool WindowsSystemWideOnly { get { return true; } }

        /// <summary>
        /// The registry path relative to HKEY_LOCAL_MACHINE which is used to store the application's capability registration information.
        /// </summary>
        [Description("The registry path relative to HKEY_LOCAL_MACHINE which is used to store the application's capability registration information.")]
        [XmlAttribute("capability-reg-path")]
        public string CapabilityRegPath { get; set; }

        /// <summary>
        /// Set to <see langword="true"/> for real 64-bit applications whose registry entries do not get redirected by WOW.
        /// </summary>
        [Description("Set to true for real 64-bit applications whose registry entries do not get redirected by WOW.")]
        [XmlAttribute("x64"), DefaultValue(false)]
        public bool X64 { get; set; }

        /// <inheritdoc/>
        [XmlIgnore]
        public override IEnumerable<string> ConflictIDs
        {
            get { return new[] {"registered-apps:" + ID, "hklm:" + CapabilityRegPath}; }
        }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns the capability in the form "AppRegistration: CapabilityRegPath". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            return string.Format("AppRegistration: {0}", CapabilityRegPath);
        }
        #endregion

        #region Clone
        /// <inheritdoc/>
        public override Capability CloneCapability()
        {
            return new AppRegistration {ID = ID, CapabilityRegPath = CapabilityRegPath, X64 = X64};
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AppRegistration other)
        {
            if (other == null) return false;

            return base.Equals(other) &&
                other.CapabilityRegPath == CapabilityRegPath && other.X64 == X64;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == typeof(AppRegistration) && Equals((AppRegistration)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ (CapabilityRegPath ?? "").GetHashCode();
                result = (result * 397) ^ X64.GetHashCode();
                return result;
            }
        }
        #endregion
    }
}