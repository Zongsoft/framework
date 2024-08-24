/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Data
{
	public class ModelPropertyDescriptorCollection : KeyedCollection<string, ModelPropertyDescriptor>
	{
		private readonly ModelDescriptor _model;

        public ModelPropertyDescriptorCollection(ModelDescriptor model) : base(StringComparer.OrdinalIgnoreCase) =>
			_model = model ?? throw new ArgumentNullException(nameof(model));

		internal int AddRange(IEnumerable<ModelPropertyDescriptor> properties)
		{
			if(properties == null)
				throw new ArgumentNullException(nameof(properties));

			int count = 0;

			foreach(var property in properties)
			{
				this.Add(property);
				count++;
			}

			return count;
		}

		protected override string GetKeyForItem(ModelPropertyDescriptor item) => item.Name;

		protected override void InsertItem(int index, ModelPropertyDescriptor item)
		{
			if(item == null)
				throw new ArgumentNullException(nameof(item));

			item.SetModel(_model);
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, ModelPropertyDescriptor item)
		{
			if(item == null)
				throw new ArgumentNullException(nameof(item));

			item.SetModel(_model);
			base.SetItem(index, item);
		}
	}
}