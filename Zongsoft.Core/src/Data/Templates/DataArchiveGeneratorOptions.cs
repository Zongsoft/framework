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
using System.Linq;

namespace Zongsoft.Data.Templates
{
	public class DataArchiveGeneratorOptions : IDataArchiveGeneratorOptions
	{
		#region 构造函数
		public DataArchiveGeneratorOptions(params string[] fields) : this(null, fields) { }
		public DataArchiveGeneratorOptions(IDataArchiveFormatter formatter, params string[] fields)
		{
			this.Formatter = formatter;

			if(fields != null && fields.Length > 0)
				this.Fields = fields.Select(field => new DataArchiveField(field)).ToArray();
		}

		public DataArchiveGeneratorOptions(params DataArchiveField[] fields) : this(null, fields) { }
		public DataArchiveGeneratorOptions(IDataArchiveFormatter formatter, params DataArchiveField[] fields)
		{
			this.Formatter = formatter;
			this.Fields = fields;
		}
		#endregion

		#region 公共属性
		public IDataArchiveFormatter Formatter { get; set; }
		public DataArchiveField[] Fields { get; set; }
		#endregion
	}
}