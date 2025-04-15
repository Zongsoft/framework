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
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Services;

public class CommandOptionCollection : IEnumerable<KeyValuePair<string, string>>
{
	#region 静态变量
	private static readonly ConcurrentDictionary<Type, CommandDescriptor> Cache = new ConcurrentDictionary<Type, CommandDescriptor>();
	#endregion

	#region 成员字段
	private bool _isBound;
	private IDictionary<string, CommandOptionAttribute> _attributes;
	private IDictionary<string, string> _items;
	#endregion

	#region 构造函数
	public CommandOptionCollection()
	{
		_items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}

	public CommandOptionCollection(IEnumerable<KeyValuePair<string, string>> items)
	{
		if(items == null)
			throw new ArgumentNullException(nameof(items));

		_items = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach(var item in items)
			_items[item.Key] = item.Value;
	}
	#endregion

	#region 公共属性
	public int Count => _items.Count;
	public ICollection<string> Keys => _items.Keys;
	public ICollection<string> Values => _items.Values;

	public string this[string key]
	{
		get
		{
			if(string.IsNullOrWhiteSpace(key))
				throw new ArgumentNullException(nameof(key));

			string value;

			if(_items.TryGetValue(key, out value))
				return value;

			throw new KeyNotFoundException($"The '{key}' command option is undefined.");
		}
	}
	#endregion

	#region 公共方法
	public bool Contains(string name) => _items.ContainsKey(name);
	public object GetValue(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(_items.TryGetValue(name, out var value))
			return value;

		if(_attributes != null && _attributes.TryGetValue(name, out var attribute))
			return attribute.DefaultValue;
		else
			throw new KeyNotFoundException($"The '{name}' command option is undefined.");
	}

	public T GetValue<T>(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(_items.TryGetValue(name, out var value))
			return Common.Convert.ConvertValue<T>(value);

		if(_attributes != null && _attributes.TryGetValue(name, out var attribute))
			return Common.Convert.ConvertValue<T>(attribute.DefaultValue);
		else
			throw new KeyNotFoundException($"The '{name}' command option is undefined.");
	}

	public bool TryGetValue(string name, out object value)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(_items.TryGetValue(name, out var result))
		{
			if(_attributes != null && _attributes.TryGetValue(name, out var attribute))
				value = attribute.Type == null ? result : Common.Convert.ConvertValue(result, attribute.Type);
			else
				value = result;

			return true;
		}

		value = null;
		return false;
	}

	public bool TryGetValue<T>(string name, out T value)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(_items.TryGetValue(name, out var result))
		{
			value = Common.Convert.ConvertValue<T>(result);
			return true;
		}

		value = default;
		return false;
	}
	#endregion

	#region 绑定方法
	public bool Bind(ICommand command)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		//如果已经绑定完成则退出
		if(_isBound)
			return false;

		//从命令描述器的缓存中获取到当前命令的描述器，如果缓存中没有则新建一个描述器并添加到缓存中
		var descriptor = Cache.GetOrAdd(command.GetType(),
		                              type => new CommandDescriptor()
		                              {
										  Metadata = (CommandAttribute)Attribute.GetCustomAttribute(type, typeof(CommandAttribute)),
										  Options = Attribute.GetCustomAttributes(type, typeof(CommandOptionAttribute), true).Cast<CommandOptionAttribute>()
									  });

		if(_attributes == null)
			System.Threading.Interlocked.CompareExchange(ref _attributes, new Dictionary<string, CommandOptionAttribute>(descriptor.Options.Count(), StringComparer.OrdinalIgnoreCase), null);

		lock(_attributes)
		{
			foreach(var attribute in descriptor.Options)
			{
				_attributes[attribute.Name] = attribute;
			}
		}

		//如果命令没有定义元数据，或者命令的元数据中声明了必须进行选项验证
		if(descriptor.Metadata == null || !descriptor.Metadata.IgnoreOptions)
		{
			//确保所有选项都是被定义的，并且它们的值都是类型正确的
			foreach(var item in _items)
			{
				this.EnsureOptionValue(item.Key, item.Value);
			}
		}

		//设置绑定已完成标志
		return _isBound = true;
	}
	#endregion

	#region 私有方法
	private void EnsureOptionValue(string name, string value)
	{
		if(_attributes == null)
			return;

		if(!_attributes.TryGetValue(name, out var attribute))
			throw new CommandOptionException($"The '{name}' command option is not declared.");

		if(attribute.Type == null)
			return;

		if(attribute.Converter != null)
		{
			if(!attribute.Converter.CanConvertFrom(typeof(string)))
				throw new CommandOptionValueException(name, value);

			try
			{
				attribute.Converter.ConvertFrom(value);
			}
			catch
			{
				throw new CommandOptionValueException(name, value);
			}
		}
		else
		{
			if(!Common.Convert.TryConvertValue(value, attribute.Type, out _))
				throw new CommandOptionValueException(name, value);
		}
	}
	#endregion

	#region 枚举遍历
	IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() => _items.GetEnumerator();
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
	#endregion

	#region 嵌套子类
	private class CommandDescriptor
	{
		public CommandAttribute Metadata;
		public IEnumerable<CommandOptionAttribute> Options;
	}
	#endregion
}
