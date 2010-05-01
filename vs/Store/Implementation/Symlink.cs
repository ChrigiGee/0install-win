﻿using System;
using System.Globalization;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementation
{
    /// <summary>
    /// An immutable symlink-entry in a <see cref="Manifest"/>.
    /// </summary>
    public sealed class Symlink : ManifestNode, IEquatable<Symlink>
    {
        #region Properties
        /// <summary>
        /// The hash of the link target path.
        /// </summary>
        public string Hash { get; set; }
        
        /// <summary>
        /// The length of the link target path.
        /// </summary>
        public long Size { get; set; }

        private string _symlinkName;
        /// <summary>
        /// The name of the symlink without the containing directory.
        /// </summary>
        public string SymlinkName
        {
            get { return _symlinkName; }
            private set
            {
                if (value.Contains("\n")) throw new ArgumentException(Resources.NewlineInName, "value");
                _symlinkName = value;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new symlink-entry.
        /// </summary>
        /// <param name="hash">The hash of the link target path.</param>
        /// <param name="size">The length of the link target path.</param>
        /// <param name="symlinkName">The name of the symlink without the containing directory.</param>
        public Symlink(string hash, long size, string symlinkName)
        {
            Hash = hash;
            Size = size;
            SymlinkName = symlinkName;
        }
        #endregion

        //--------------------//

        #region Conversion
        /// <summary>
        /// Returns the string representation of this node for the manifest format.
        /// </summary>
        /// <returns><code>"S", space, hash, space, size, space, symlink name, newline</code></returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "S {0} {1} {2}\n", Hash, Size, SymlinkName);
        }
        #endregion

        #region Compare
        public bool Equals(Symlink other)
        {
            if (other == null) return false;
            if (other == this) return true;
            return Equals(other.Hash, Hash) && other.Size == Size && Equals(other.SymlinkName, SymlinkName);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(obj, this)) return true;
            return obj.GetType() == typeof(Symlink) && Equals((Symlink)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (Hash != null ? Hash.GetHashCode() : 0);
                result = (result * 397) ^ Size.GetHashCode();
                result = (result * 397) ^ (SymlinkName != null ? SymlinkName.GetHashCode() : 0);
                return result;
            }
        }

        public static bool operator ==(Symlink left, Symlink right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Symlink left, Symlink right)
        {
            return !Equals(left, right);
        }
        #endregion
    }
}
