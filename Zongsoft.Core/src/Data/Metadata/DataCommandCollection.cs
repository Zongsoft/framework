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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Metadata
{
	public class DataCommandCollection() : KeyedCollection<string, IDataCommand>(StringComparer.OrdinalIgnoreCase)
	{
		#region 公共属性
		public IDataCommand this[string name, string @namespace = null] => base[GetKey(name, @namespace)];
		#endregion

		#region 公共方法
		public bool TryAdd(IDataCommand command)
		{
			if(command == null)
				throw new ArgumentNullException(nameof(command));

			if(this.Contains(command))
				return false;

			this.Add(command);
			return true;
		}

		public bool Contains(string name, string @namespace = null) => base.Contains(GetKey(name, @namespace));
		public bool Remove(string name, string @namespace = null) => base.Remove(GetKey(name, @namespace));
		public bool TryGetValue(string name, string @namespace, out IDataCommand value) => base.TryGetValue(GetKey(name, @namespace), out value);
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(IDataCommand command) => command.QualifiedName;
		#endregion

		#region 私有方法
		private static string GetKey(string name, string @namespace) => string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
		#endregion
	}
}
