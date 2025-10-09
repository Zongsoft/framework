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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Zongsoft.Components;

partial class CommandLine
{
	public static CmdletOptionCollection GetOptions(CommandDescriptor descriptor, IEnumerable<CmdletOption> options)
	{
		return new CmdletOptionCollection(descriptor, options);
	}

	public static T GetOptions<T>(CommandDescriptor descriptor, IEnumerable<CmdletOption> options)
	{
		if(descriptor == null)
			throw new ArgumentNullException(nameof(descriptor));

		var result = Activator.CreateInstance<T>();

		if(options == null)
			return result;

		foreach(var option in options)
		{
			if(option.Kind == CmdletOptionKind.Fully)
			{
				if(descriptor.Options.TryGetValue(option.Name, out var optionDescriptor))
					Reflection.Reflector.SetValue(ref result, optionDescriptor.Name, type => Common.Convert.ConvertValue(option.Value, type, () => optionDescriptor.ConverterType == null ? null : TypeDescriptor.GetConverter(optionDescriptor.ConverterType), optionDescriptor.DefaultValue));
			}
			else
			{
				foreach(var character in option.Name)
				{
					if(descriptor.Options.TryGetValue(character.ToString(), out var optionDescriptor))
						Reflection.Reflector.SetValue(ref result, optionDescriptor.Name, type => Common.Convert.ConvertValue(option.Value, type, () => optionDescriptor.ConverterType == null ? null : TypeDescriptor.GetConverter(optionDescriptor.ConverterType), optionDescriptor.DefaultValue));
				}
			}
		}

		return result;
	}

	public sealed class CmdletOptionCollection : IReadOnlyCollection<KeyValuePair<string, object>>
	{
		#region 成员字段
		private readonly int _count;
		private readonly CommandDescriptor _descriptor;
		private readonly Dictionary<string, object> _options;
		#endregion

		#region 私有构造
		internal CmdletOptionCollection(CommandDescriptor descriptor, IEnumerable<CmdletOption> options)
		{
			_descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
			_options = options == null ? [] : new Dictionary<string, object>();

			foreach(var option in options)
			{
				if(option.Kind == CmdletOptionKind.Fully)
				{
					_count++;
					Populate(option.Name, option.Value);
				}
				else
				{
					foreach(var character in option.Name)
					{
						_count++;
						Populate(character.ToString(), option.Value);
					}
				}
			}

			void Populate(string optionName, string optionValue)
			{
				if(_descriptor.Options.TryGetValue(optionName, out var optionDescriptor))
				{
					_options[optionDescriptor.Name] = Common.Convert.ConvertValue(
						optionValue,
						optionDescriptor.Type,
						() => optionDescriptor.ConverterType == null ? null : TypeDescriptor.GetConverter(optionDescriptor.ConverterType),
						optionDescriptor.DefaultValue);

					if(optionDescriptor.Symbol != '\0')
						_options[optionDescriptor.Symbol.ToString()] = _options[optionDescriptor.Name];
				}
				else
					_options[optionName] = optionValue;
			}
		}
		#endregion

		#region 公共属性
		public int Count => _count;
		#endregion

		#region 公共方法
		public bool TryGetValue(string name, out object value)
		{
			if(string.IsNullOrEmpty(name))
			{
				value = null;
				return false;
			}

			if(_options.TryGetValue(name, out value))
				return true;

			if(_descriptor.Options.TryGetValue(name, out var descriptor))
			{
				value = descriptor.DefaultValue;
				return true;
			}

			value = null;
			return false;
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _options.GetEnumerator();
		#endregion
	}
}
