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

namespace Zongsoft.Plugins;

public abstract class FixedElement : PluginElement
{
	#region 私有变量
	private readonly object _syncRoot;
	#endregion

	#region 成员变量
	private Type _type;
	private string _typeName;
	private FixedElementType _fixedElementType;
	private object _value;
	#endregion

	#region 构造函数
	protected FixedElement(Type type, string name, Plugin plugin, FixedElementType elementType) : base(name, plugin)
	{
		if(plugin == null)
			throw new ArgumentNullException(nameof(plugin));
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		_syncRoot = new object();
		_type = type;
		_fixedElementType = elementType;
	}

	protected FixedElement(string typeName, string name, Plugin plugin, FixedElementType elementType) : base(name, plugin)
	{
		if(plugin == null)
			throw new ArgumentNullException(nameof(plugin));
		if(string.IsNullOrWhiteSpace(typeName))
			throw new ArgumentNullException(nameof(typeName));

		_syncRoot = new object();
		_typeName = typeName;
		_fixedElementType = elementType;
	}
	#endregion

	#region 公共属性
	public Type Type
	{
		get
		{
			if(_type == null)
			{
				lock(_syncRoot)
				{
					if(_type == null)
					{
						var type = PluginUtility.GetType(_typeName, this);

						if(!this.ValidateType(type))
							throw new InvalidOperationException();

						_type = type;
					}
				}
			}

			return _type;
		}
	}

	public FixedElementType FixedElementType => _fixedElementType;
	public bool HasValue => _value != null;
	#endregion

	#region 保护方法
	internal protected object GetValue()
	{
		if(_value == null)
		{
			lock(_syncRoot)
			{
				_value ??= this.CreateValue();
			}
		}

		return _value;
	}
	#endregion

	#region 虚拟方法
	protected virtual bool ValidateType(Type type) => type != null;
	protected virtual object CreateValue()
	{
		if(this.Type == null)
			return null;

		try
		{
			var result = PluginUtility.BuildType(this.Type, this);

			if(result == null)
				throw new PluginException(string.Format(Properties.Resources.Plugin_ConstructorNotFound_Message, this.Type.FullName, this.Plugin));

			return result;
		}
		catch(Exception ex)
		{
			throw new PluginException(string.Format(Properties.Resources.Plugin_InstanceCreationFailed_Message, this.Type.FullName, this.Plugin), ex);
		}
	}
	#endregion
}
