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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.ComponentModel;

namespace Zongsoft.Plugins;

public class PluginElementProperty
{
	#region 成员变量
	private string _name;
	private object _value;
	private string _rawValue;
	private PluginTreeNode _valueNode;
	private PluginElement _owner;
	#endregion

	#region 私有变量
	private int _valueEvaluated;
	#endregion

	#region 构造函数
	internal protected PluginElementProperty(PluginElement owner, string name, string rawValue)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name.Trim();
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		_rawValue = rawValue;
	}

	internal protected PluginElementProperty(PluginElement owner, string name, PluginTreeNode valueNode)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name.Trim();
		_owner = owner ?? throw new ArgumentNullException(nameof(owner));
		_valueNode = valueNode ?? throw new ArgumentNullException(nameof(valueNode));
		_rawValue = valueNode.FullPath;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前属性的名称。</summary>
	public string Name => _name;

	/// <summary>获取或设置当前属性的原生文本值。</summary>
	/// <remarks>如果该属性发生改变，在下次获取<see cref="Value"/>属性时将自动引发重新计算。</remarks>
	public string RawValue
	{
		get => _rawValue;
		set
		{
			if(string.Equals(_rawValue, value, StringComparison.Ordinal))
				return;

			_rawValue = value;
			_valueEvaluated = 0;
		}
	}

	/// <summary>获取当前属性的所有者。</summary>
	public PluginElement Owner
	{
		get => _owner;
		internal set => _owner = value ?? throw new ArgumentNullException();
	}

	/// <summary>获取当前属性的值。</summary>
	/// <remarks>注意：当该属性值被计算过后就不在重复计算。</remarks>
	public object Value
	{
		get
		{
			var valueEvaluated = Interlocked.CompareExchange(ref _valueEvaluated, 1, 0);

			if(valueEvaluated == 0)
				_value = this.GetValue(this.Type, null);

			return _value;
		}
	}

	/// <summary>获取或设置属性类型。</summary>
	public Type Type { get; set; }

	/// <summary>获取或设置类型转换器。</summary>
	public TypeConverter Converter { get; set; }
	#endregion

	#region 公共方法
	public object GetValue(Type valueType) => this.GetValue(
		valueType ?? this.Type,
		valueType == null ? null : Common.TypeExtension.GetDefaultValue(valueType));

	public object GetValue(Type valueType, object defaultValue)
	{
		if(_valueNode == null)
			return PluginUtility.ResolveValue(_owner, _rawValue, _name, valueType, this.Converter, defaultValue);

		var result = _valueNode.UnwrapValue(ObtainMode.Auto, new Builders.BuilderSettings(valueType));

		if(valueType != null)
			result = Common.Convert.ConvertValue(result, valueType, () => this.Converter, defaultValue);

		return result;
	}
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name}={this.RawValue}";
	#endregion
}
