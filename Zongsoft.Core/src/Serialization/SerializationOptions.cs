﻿/*
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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Serialization
{
	public class SerializationOptions
	{
		#region 成员字段
		private int _maximumDepth;
		#endregion

		#region 公共属性
		/// <summary>获取或设置最大的序列化深度，默认为零(不限制)。</summary>
		public int MaximumDepth
		{
			get => _maximumDepth;
			set => _maximumDepth = Math.Max(0, value);
		}

		/// <summary>获取或设置一个值，指示是否忽略空值(null)。</summary>
		public bool IgnoreNull { get; set; }

		/// <summary>获取或设置一个值，指示是否忽略空集和空字符串。</summary>
		public bool IgnoreEmpty { get; set; }

		/// <summary>获取或设置一个值，指示是否忽略零。</summary>
		public bool IgnoreZero { get; set; }

		/// <summary>获取或设置一个值，指示是否包含字段。</summary>
		public bool IncludeFields { get; set; }
		#endregion

		#region 公共方法
		public SerializationOptions Ignores(string text)
		{
			if(string.IsNullOrEmpty(text))
				return this;

			text = text.Trim();

			if(text == "*")
			{
				this.IgnoreEmpty = true;
				this.IgnoreNull = true;
				this.IgnoreZero = true;
			}
			else
			{
				foreach(var part in Common.StringExtension.Slice(text, ',', '|'))
				{
					if(string.Equals(part, "null", StringComparison.OrdinalIgnoreCase))
						this.IgnoreNull = true;
					else if(string.Equals(part, "empty", StringComparison.OrdinalIgnoreCase))
						this.IgnoreEmpty = true;
					else if(string.Equals(part, "zero", StringComparison.OrdinalIgnoreCase))
						this.IgnoreZero = true;
				}
			}

			return this;
		}
		#endregion
	}
}
