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

	/// <summary>表示命令选项集合，并提供可控制默认值回退的变量查询。</summary>
	/// <remarks>
	/// 	<para>选项名称和短名称均按 <see cref="StringComparer.OrdinalIgnoreCase"/> 比较；普通选项查询优先返回已传入的值，其次返回选项描述中的默认值，存在且值为 <see langword="null"/> 时仍表示查询成功。</para>
	/// 	<para>通过 <see cref="Common.IVariables"/> 查询时只提供全局变量，<see langword="null"/> 或空字符串命名空间均表示全局。没有回退参数的重载等价于 <c>fallback=false</c>，只查询全局显式参数；<c>fallback=true</c> 允许从具名命名空间回退到全局，并在显式参数不存在时查询已声明的默认值。命名空间不用于指定查询模式。</para>
	/// 	<para>变量名称将选项名称中的点号和连字符转换为下划线，例如 <c>install-path</c> 和 <c>install.path</c> 均对应 <c>install_path</c>。转换后的名称必须符合 <c>[A-Za-z_][A-Za-z0-9_]*</c>，否则忽略该选项及其短名称。</para>
	/// 	<para>变量查询参数必须是合法的变量名称，不进行裁剪、转换或命名空间拆分。普通选项查询方法使用原始选项名称并允许默认值回退，不受变量查询的回退设置影响；合法的短名称也可用于变量查询。</para>
	/// 	<para>显式参数与选项描述独立保留：各层映射重名均按枚举顺序返回首个匹配项。仅 <see cref="CommandOptionDescriptor.HasDefaultValue"/> 为 <see langword="true"/> 时存在默认值，显式声明的 <see langword="null"/> 也属于默认值；显式参数存在时立即返回，不再回退。</para>
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

			if(_descriptor.Options.TryGetValue(name, out var option) && option.HasDefaultValue)
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

			if(_descriptor.Options.TryGetValue(name, out var descriptor) && descriptor.HasDefaultValue)
				return descriptor.DefaultValue;

			throw new ArgumentException(string.Format(Properties.Resources.CommandOption_NotFound_Message, name));
		}

		public T GetValue<T>(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_options.TryGetValue(name, out var value) && value != null)
				return Common.Convert.ConvertValue<T>(value);

			if(_descriptor.Options.TryGetValue(name, out var descriptor) && descriptor.HasDefaultValue)
				return Common.Convert.ConvertValue<T>(descriptor.DefaultValue, descriptor.GetConverter);

			throw new ArgumentException(string.Format(Properties.Resources.CommandOption_NotFound_Message, name));
		}

		public T GetValue<T>(string name, T defaultValue)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if(_options.TryGetValue(name, out var value))
				return value == null ? defaultValue : Common.Convert.ConvertValue<T>(value, defaultValue);

			if(_descriptor.Options.TryGetValue(name, out var descriptor) && descriptor.HasDefaultValue)
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

			if(_descriptor.Options.TryGetValue(name, out var descriptor) && descriptor.HasDefaultValue)
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

			if(_descriptor.Options.TryGetValue(name, out var descriptor) && descriptor.HasDefaultValue)
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
		bool Common.IVariables.TryGetValue(string name, out object value) => ((Common.IVariables)this).TryGetValue(null, name, false, out value);
		bool Common.IVariables.TryGetValue(string name, bool fallback, out object value) => ((Common.IVariables)this).TryGetValue(null, name, fallback, out value);
		bool Common.IVariables.TryGetValue(string @namespace, string name, out object value) => ((Common.IVariables)this).TryGetValue(@namespace, name, false, out value);
		bool Common.IVariables.TryGetValue(string @namespace, string name, bool fallback, out object value)
		{
			ArgumentNullException.ThrowIfNull(name);
			value = null;

			if(!Expressions.Tokenization.IdentifierTokenizer.IsIdentifier(name) || !fallback && !string.IsNullOrEmpty(@namespace))
				return false;

			var variables = System.Threading.Volatile.Read(ref _variables) ??
				System.Threading.LazyInitializer.EnsureInitialized(ref _variables, this.CreateVariables);

			if(!variables.TryGetValue(name, out var entry))
				return false;

			if(entry.Specified)
			{
				value = entry.Value;
				return true;
			}

			if(fallback && entry.Descriptor != null && entry.Descriptor.HasDefaultValue)
			{
				value = entry.Descriptor.DefaultValue;
				return true;
			}

			return false;
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
					variables.TryAdd(name, new(true, option.Value));
			}

			foreach(var descriptor in _descriptor.Options)
			{
				var name = GetVariableName(descriptor.Name);
				if(name == null)
					continue;

				AddDescriptor(name, descriptor);
				if(descriptor.Symbol != '\0')
					AddDescriptor(GetVariableName(descriptor.Symbol.ToString()), descriptor);
			}

			return variables;

			void AddDescriptor(string name, CommandOptionDescriptor descriptor)
			{
				if(name == null)
					return;

				if(variables.TryGetValue(name, out var entry))
				{
					if(entry.Descriptor == null)
						variables[name] = new(entry.Specified, entry.Value, descriptor);
				}
				else
					variables.Add(name, new(false, null, descriptor));
			}
		}

		private static string GetVariableName(string name)
		{
			name = name?.Replace('.', '_').Replace('-', '_');
			return Expressions.Tokenization.IdentifierTokenizer.IsIdentifier(name) ? name : null;
		}
		#endregion

		#region 嵌套结构
		private readonly struct VariableEntry(bool specified, object value, CommandOptionDescriptor descriptor = null)
		{
			public readonly bool Specified = specified;
			public readonly object Value = value;
			public readonly CommandOptionDescriptor Descriptor = descriptor;
		}
		#endregion
	}
}
