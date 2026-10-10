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

namespace Zongsoft.Components;

partial class CommandLine
{
	#region 静态方法
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
	#endregion

	/// <summary>表示命令选项集合，并提供默认命名空间中的变量查询。</summary>
	/// <remarks>
	/// 	<para>选项名称和短名称均按 <see cref="StringComparer.OrdinalIgnoreCase"/> 比较；查询优先返回已传入的值，其次返回选项描述中的默认值，存在且值为 <see langword="null"/> 时仍表示查询成功。</para>
	/// 	<para>通过 <see cref="Common.IVariables"/> 只提供默认命名空间；<see langword="null"/> 和空字符串命名空间等价，非空命名空间查询失败，不回退。</para>
	/// 	<para>变量名称将选项名称中的点号和连字符转换为下划线，例如 <c>install-path</c> 和 <c>install.path</c> 均对应 <c>install_path</c>。转换后的名称必须符合 <c>[A-Za-z_][A-Za-z0-9_]*</c>，否则忽略该选项及其短名称。</para>
	/// 	<para>变量查询参数必须是合法的变量名称，不进行裁剪、转换或命名空间拆分。原有选项查询方法仍使用原始选项名称，合法的短名称也可用于变量查询。</para>
	/// 	<para>映射重名时优先匹配已传入的选项，同一层级按枚举顺序返回首个匹配项；未匹配已传入的选项时，按描述集合顺序查询默认值。</para>
	/// 	<para>变量名称索引在首次有效变量查询时创建并固定，随后增删或替换选项描述不会更新索引；默认值仍从原描述对象实时读取。索引安全发布后只读，不提供描述集合并发修改的同步保证。</para>
	/// </remarks>
	public sealed partial class CmdletOptionCollection : IReadOnlyCollection<KeyValuePair<string, object>>
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
			_options = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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
					if(optionValue == null)
						_options[optionDescriptor.Name] = null;
					else if(Common.Convert.TryConvertValue(optionValue, optionDescriptor.Type, optionDescriptor.GetConverter, out var convertedValue))
						_options[optionDescriptor.Name] = convertedValue;
					else
						throw new CommandOptionValueException(optionName, optionValue);

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
			if(string.IsNullOrEmpty(name))
				return false;

			if(_options.TryGetValue(name, out var value))
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

			if(_descriptor.Options.TryGetValue(name, out var option))
			{
				if(option.DefaultValue == null)
					return false;

				if(option.Type == typeof(bool))
					return Common.Convert.ConvertValue<bool>(option.DefaultValue);
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

			throw new ArgumentException(string.Format(Properties.Resources.CommandOption_NotFound_Message, name));
		}

		public T GetValue<T>(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_options.TryGetValue(name, out var value) && value != null)
				return Common.Convert.ConvertValue<T>(value);

			if(_descriptor.Options.TryGetValue(name, out var descriptor))
				return Common.Convert.ConvertValue<T>(descriptor.DefaultValue, descriptor.GetConverter);

			throw new ArgumentException(string.Format(Properties.Resources.CommandOption_NotFound_Message, name));
		}

		public T GetValue<T>(string name, T defaultValue)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_options.TryGetValue(name, out var value))
				return value == null ? defaultValue : Common.Convert.ConvertValue<T>(value, defaultValue);

			if(_descriptor.Options.TryGetValue(name, out var descriptor) && descriptor.DefaultValue != null)
				return Common.Convert.ConvertValue(descriptor.DefaultValue, descriptor.GetConverter, defaultValue);

			return defaultValue;
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

	partial class CmdletOptionCollection : Common.IVariables
	{
		#region 成员字段
		private Dictionary<string, VariableEntry> _variables;
		#endregion

		#region 显式实现
		bool Common.IVariables.TryGetValue(string name, out object value) => ((Common.IVariables)this).TryGetValue(null, name, out value);
		bool Common.IVariables.TryGetValue(string @namespace, string name, out object value)
		{
			value = null;

			if(!string.IsNullOrEmpty(@namespace) || !Expressions.Tokenization.IdentifierTokenizer.IsIdentifier(name))
				return false;

			var variables = System.Threading.Volatile.Read(ref _variables) ??
				System.Threading.LazyInitializer.EnsureInitialized(ref _variables, this.CreateVariables);

			if(!variables.TryGetValue(name, out var entry))
				return false;

			value = entry.Descriptor == null ? entry.Value : entry.Descriptor.DefaultValue;
			return true;
		}
		#endregion

		#region 私有方法
		private Dictionary<string, VariableEntry> CreateVariables()
		{
			var variables = new Dictionary<string, VariableEntry>(_options.Count + _descriptor.Options.Count, StringComparer.OrdinalIgnoreCase);

			foreach(var option in _options)
			{
				if(_descriptor.Options.TryGetValue(option.Key, out var descriptor) && GetVariableName(descriptor.Name) == null)
					continue;

				var name = GetVariableName(option.Key);

				if(name != null)
					variables.TryAdd(name, new(option.Value));
			}

			foreach(var descriptor in _descriptor.Options)
			{
				var name = GetVariableName(descriptor.Name);

				if(name == null)
					continue;

				var entry = new VariableEntry(null, descriptor);
				variables.TryAdd(name, entry);

				if(descriptor.Symbol != '\0')
				{
					name = GetVariableName(descriptor.Symbol.ToString());

					if(name != null)
						variables.TryAdd(name, entry);
				}
			}

			return variables;
		}

		private static string GetVariableName(string name)
		{
			name = name?.Replace('.', '_').Replace('-', '_');
			return Expressions.Tokenization.IdentifierTokenizer.IsIdentifier(name) ? name : null;
		}
		#endregion

		#region 嵌套结构
		private readonly struct VariableEntry(object value, CommandOptionDescriptor descriptor = null)
		{
			public readonly object Value = value;
			public readonly CommandOptionDescriptor Descriptor = descriptor;
		}
		#endregion
	}
}
