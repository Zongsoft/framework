/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Data.Templates
{
	public class DataArchiveField
	{
		#region 构造函数
		public DataArchiveField(string name, string label = null, string description = null)
        {
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

            this.Name = name;
			this.Label = label;
			this.Description = description;
        }
		#endregion

		#region 公共属性
		public string Name { get; }
		public string Label { get; set; }
		public string Description { get; set; }
		#endregion

		#region 重写方法
		public override string ToString() => string.IsNullOrEmpty(this.Label) || string.Equals(this.Name, this.Label) ?
			this.Name : $"{this.Name}({this.Label})";
		#endregion
	}
}