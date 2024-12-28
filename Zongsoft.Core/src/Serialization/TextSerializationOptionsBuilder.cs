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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Concurrent;

namespace Zongsoft.Serialization;

public class TextSerializationOptionsBuilder
{
	#region 常量定义
	private const uint IMMUTABLE_FLAG = 0x80_00_00_00;
	private const uint TYPIFIED_FLAG = 0x40_00_00_00;
	private const uint INDENTED_FLAG = 0x20_00_00_00;
	private const uint NAMING_CAMEL_FLAG = 0x01_00_00_00;
	private const uint NAMING_PASCAL_FLAG = 0x02_00_00_00;
	private const uint INCLUDE_FIELDS_FLAG = 0x00_01_00_00;
	#endregion

	#region 私有字段
	private static readonly ConcurrentDictionary<uint, TextSerializationOptions> _options = new();
	#endregion

	#region 公共方法
	public TextSerializationOptions Indented(string ignores = null) => Indented(false, ignores);
	public TextSerializationOptions Indented(bool typified, string ignores = null)
	{
		var flags = IMMUTABLE_FLAG | INDENTED_FLAG;
		flags |= typified ? TYPIFIED_FLAG : 0;
		flags |= SerializationOptions.GetIgnoring(ignores);

		return _options.GetOrAdd(flags, (key, state) =>
		{
			var options = new TextSerializationOptions()
			{
				Typified = true,
			};

			//设置忽略项
			options.Ignores(state);

			//使构建的选项不能变更
			return options.Immutate();
		}, ignores);
	}

	public TextSerializationOptions Typified(string ignores = null) => Typified(false, ignores);
	public TextSerializationOptions Typified(bool indented, string ignores = null)
	{
		var flags = IMMUTABLE_FLAG | TYPIFIED_FLAG;
		flags |= indented ? INDENTED_FLAG : 0;
		flags |= SerializationOptions.GetIgnoring(ignores);

		return _options.GetOrAdd(flags, (key, state) =>
		{
			var options = new TextSerializationOptions()
			{
				Typified = true,
			};

			//设置忽略项
			options.Ignores(state);

			//使构建的选项不能变更
			return options.Immutate();
		}, ignores);
	}

	public TextSerializationOptions Camel(string ignores = null) => Camel(false, ignores);
	public TextSerializationOptions Camel(bool typified, string ignores = null)
	{
		var flags = IMMUTABLE_FLAG | NAMING_CAMEL_FLAG;
		flags |= typified ? TYPIFIED_FLAG : 0;
		flags |= SerializationOptions.GetIgnoring(ignores);

		return _options.GetOrAdd(flags, (key, state) =>
		{
			var options = new TextSerializationOptions()
			{
				Typified = state.typified,
				NamingConvention = SerializationNamingConvention.Camel,
			};

			//设置忽略项
			options.Ignores(state.ignores);

			//使构建的选项不能变更
			return options.Immutate();
		}, (typified, ignores));
	}

	public TextSerializationOptions Pascal(string ignores = null) => Pascal(false, ignores);
	public TextSerializationOptions Pascal(bool typified, string ignores = null)
	{
		var flags = IMMUTABLE_FLAG | NAMING_PASCAL_FLAG;
		flags |= typified ? TYPIFIED_FLAG : 0;
		flags |= SerializationOptions.GetIgnoring(ignores);

		return _options.GetOrAdd(flags, (key, state) =>
		{
			var options = new TextSerializationOptions()
			{
				Typified = state.typified,
				NamingConvention = SerializationNamingConvention.Pascal,
			};

			//设置忽略项
			options.Ignores(state.ignores);

			//使构建的选项不能变更
			return options.Immutate();
		}, (typified, ignores));
	}
	#endregion
}
