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
					Reflection.Reflector.SetValue(ref result, optionDescriptor.Name, type => Common.Convert.ConvertValue(option.Value, type, () => optionDescriptor.GetConverter(), optionDescriptor.DefaultValue));
			}
			else
			{
				foreach(var character in option.Name)
				{
					if(descriptor.Options.TryGetValue(character.ToString(), out var optionDescriptor))
						Reflection.Reflector.SetValue(ref result, optionDescriptor.Name, type => Common.Convert.ConvertValue(option.Value, type, () => optionDescriptor.GetConverter(), optionDescriptor.DefaultValue));
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
					_options[optionDescriptor.Name] = optionValue == null ? null : Common.Convert.ConvertValue(
						optionValue,
						optionDescriptor.Type,
						optionDescriptor.GetConverter,
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
		public ICollection<string> Keys => _options.Keys;
		public ICollection<object> Values => _options.Values;
		public object this[string name] => this.GetValue(name);
		#endregion

		#region 公共方法
		public bool Contains(string name) => name != null && _options.ContainsKey(name);
		public bool Switch(string name)
		{
			if(name != null && _options.TryGetValue(name, out var value))
			{
				if(value == null)
					return true;

				if(Common.Convert.TryConvertValue<bool>(value, out var result))
					return result;

				return value is string text &&
				(
					string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(text, "on", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(text, "enable", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(text, "enabled", StringComparison.OrdinalIgnoreCase)
				);
			}

			return false;
		}

		public object GetValue(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_options.TryGetValue(name, out var value))
				return value;

			if(_descriptor.Options.TryGetValue(name, out var descriptor))
				return descriptor.DefaultValue;

			throw new ArgumentException($"The command option named '{name}' was not found.");
		}

		public T GetValue<T>(string name, T defaultValue = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_options.TryGetValue(name, out var value))
				return value == null ? defaultValue : Common.Convert.ConvertValue<T>(value, defaultValue);

			if(_descriptor.Options.TryGetValue(name, out var descriptor))
				return Common.Convert.ConvertValue<T>(descriptor.DefaultValue, descriptor.GetConverter);

			throw new ArgumentException($"The command option named '{name}' was not found.");
		}

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

		public bool TryGetValue<T>(string name, out T value)
		{
			if(string.IsNullOrEmpty(name))
			{
				value = default;
				return false;
			}

			if(_options.TryGetValue(name, out var obj))
				return Common.Convert.TryConvertValue<T>(obj, out value);

			if(_descriptor.Options.TryGetValue(name, out var descriptor))
				return Common.Convert.TryConvertValue<T>(descriptor.DefaultValue, descriptor.GetConverter, out value);

			value = default;
			return false;
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _options.GetEnumerator();
		#endregion
	}
}
