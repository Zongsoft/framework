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

namespace Zongsoft.Components;

public class EventContext
{
	#region 构造函数
	public EventContext(EventRegistryBase registry, string name, Collections.Parameters parameters = null)
	{
		this.Registry = registry ?? throw new ArgumentNullException(nameof(registry));
		this.Name = name;
		this.Descriptor = registry.Events.TryGetValue(name, out var descriptor) ? descriptor : null;
		this.QualifiedName = string.IsNullOrEmpty(registry.Name) ? name : $"{registry.Name}:{name}";
		this.Parameters = parameters ?? new Collections.Parameters();
	}
	#endregion

	#region 公共属性
	/// <summary>获取事件注册表。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public EventRegistryBase Registry { get; }

	/// <summary>获取发布时的声明参数类型。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public virtual Type ArgumentType => typeof(object);

	/// <summary>获取对应的事件描述器。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public EventDescriptor Descriptor { get; }

	public string Name { get; }
	public string QualifiedName { get; }
	public Collections.Parameters Parameters { get; }
	#endregion

	#region 内部方法
	internal virtual object GetArgumentValue() => null;
	#endregion
}

public class EventContext<TArgument> : EventContext
{
	#region 构造函数
	public EventContext(EventRegistryBase registry, string name, TArgument argument, Collections.Parameters parameters = null) : base(registry, name, parameters)
	{
		this.Argument = argument;
	}
	#endregion

	#region 公共属性
	public TArgument Argument { get; set; }
	public override Type ArgumentType => typeof(TArgument);
	internal override object GetArgumentValue() => this.Argument;
	#endregion
}
