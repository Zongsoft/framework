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

namespace Zongsoft.Components.States;

public abstract class State<TKey, TValue> : IEquatable<State<TKey, TValue>> where TKey : struct, IEquatable<TKey> where TValue : struct
{
	#region 构造函数
	protected State(IStateDiagram<TKey, TValue> diagram, TKey key, TValue value)
	{
		this.Diagram = diagram ?? throw new ArgumentNullException(nameof(diagram));
		this.Key = key;
		this.Value = value;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前状态所属的<see cref="IStateDiagram{TKey, TValue}"/>状态图。</summary>
	public IStateDiagram<TKey, TValue> Diagram { get; }

	/// <summary>获取当前状态的键值。</summary>
	public TKey Key { get; }

	/// <summary>获取或设置当前状态值。</summary>
	public TValue Value { get; set; }
	#endregion

	#region 重写方法
	public bool Equals(State<TKey, TValue> state) => state != null && this.Key.Equals(state.Key) && this.Value.Equals(state.Value);
	public override bool Equals(object obj) => obj is State<TKey, TValue> other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Key, this.Value);
	public override string ToString() => $"{this.Key}:{this.Value}";
	#endregion
}
