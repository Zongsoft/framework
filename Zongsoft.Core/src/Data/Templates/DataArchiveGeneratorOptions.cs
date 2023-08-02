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
	public class DataArchiveGeneratorOptions : IDataArchiveGeneratorOptions
	{
		#region 构造函数
		public DataArchiveGeneratorOptions(params DataArchiveField[] fields) : this(null, fields) { }
		public DataArchiveGeneratorOptions(IDataArchiveFormatter formatter, params DataArchiveField[] fields)
		{
			this.Formatter = formatter;
			this.Fields = fields;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置生成格式化器。</summary>
		public IDataArchiveFormatter Formatter { get; set; }
		/// <inheritdoc />
		public DataArchiveField[] Fields { get; set; }
		#endregion

		#region 公共方法
		public virtual object Format(object target, ModelPropertyDescriptor property)
		{
			var formatter = this.Formatter;

			if(formatter == null)
				return Reflection.Reflector.GetValue(property.Member, ref target);
			else
				return formatter.Format(target, property);
		}
		#endregion
	}
}