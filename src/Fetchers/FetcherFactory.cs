﻿/*
 * Copyright 2010-2013 Bastian Eicher, Roland Leopold Walkling
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
using ZeroInstall.Store.Implementation;

namespace ZeroInstall.Fetchers
{
    /// <summary>
    /// Creates <see cref="IFetcher"/> instances.
    /// </summary>
    public static class FetcherFactory
    {
        /// <summary>
        /// Creates an <see cref="IFetcher"/> instance that uses <see cref="StoreFactory.CreateDefault"/>.
        /// </summary>
        /// <exception cref="IOException">Thrown if there was a problem accessing a configuration file or one of the stores.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if access to a configuration file or one of the stores was not permitted.</exception>
        public static IFetcher CreateDefault()
        {
            return new SequentialFetcher(StoreFactory.CreateDefault());
        }
    }
}