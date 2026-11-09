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

public class SupervisableOptions
{
	#region 常量定义
	private const int ERROR_LIMIT = -1;
	#endregion

	#region 成员字段
	private int _errorLimit;
	private TimeSpan _lifecycle;
	#endregion

	#region 构造函数
	public SupervisableOptions(int errorLimit = ERROR_LIMIT) : this(TimeSpan.Zero, errorLimit) { }
	public SupervisableOptions(TimeSpan lifecycle, int errorLimit = ERROR_LIMIT)
	{
		this.ErrorLimit = errorLimit;
		this.Lifecycle = lifecycle > TimeSpan.Zero ? lifecycle : TimeSpan.FromSeconds(60);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置监测的生命周期，如果被观察对象超过该周期内未有汇报，则会被驱逐监测。如果为零表示不启用该限制，默认值为<c>60</c>秒。</summary>
	public TimeSpan Lifecycle
	{
		get => _lifecycle;
		set => _lifecycle = value;
	}

	/// <summary>获取或设置错误次数的限值，如果被观察对象汇报的异常超过该限定值，则可能会被驱逐监测。默认为<c>-1</c>，表示不启用该限制。</summary>
	public int ErrorLimit
	{
		get => _errorLimit;
		set => _errorLimit = Math.Max(value, -1);
	}
	#endregion

	#region 内部方法
	internal bool HasErrorLimit(out int limit)
	{
		limit = _errorLimit;
		return limit > -1;
	}
	#endregion

	#region 重写方法
	public override string ToString() => $"{nameof(this.Lifecycle)}={(this.Lifecycle > TimeSpan.Zero ? this.Lifecycle : "Infinite")};{nameof(this.ErrorLimit)}={this.ErrorLimit}";
	#endregion
}
