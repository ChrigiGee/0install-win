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

using ZeroInstall.Model;

namespace ZeroInstall.Publish.Commands
{
    /// <summary>
    /// Command that adds a category to <see cref="Feed.Categories"/>.
    /// </summary>
    public sealed class AddCategory : FeedCommand
    {
        #region Variables
        private readonly string _category;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new command for adding a category to <see cref="Feed.Categories"/>.
        /// </summary>
        /// <param name="feed">The <see cref="Feed"/> to be modified.</param>
        /// <param name="category">The category to be added.</param>
        public AddCategory(Feed feed, string category) : base(feed)
        {
            _category = category;
        }
        #endregion

        //--------------------//

        #region Execute
        /// <summary>
        /// Adds the category to <see cref="Feed.Categories"/>.
        /// </summary>
        protected override void OnExecute()
        {
            Feed.Categories.Add(_category);
        }
        #endregion

        #region Undo
        /// <summary>
        /// Removes the category from <see cref="Feed.Categories"/>.
        /// </summary>
        protected override void OnUndo()
        {
            Feed.Categories.Remove(_category);
        }
        #endregion
    }
}
