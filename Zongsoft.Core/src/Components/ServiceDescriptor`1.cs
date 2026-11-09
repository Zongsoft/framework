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
using System.Collections.Generic;

namespace Zongsoft.Components;

public class ServiceDescriptor<TOperations> : ServiceDescriptor where TOperations : IReadOnlyCollection<ServiceDescriptor.Operation>
{
	#region 构造函数
	protected ServiceDescriptor() { }
	public ServiceDescriptor(Type type, string name = null, string qualifiedName = null) : base(type, name, qualifiedName) { }
	#endregion

	#region 公共属性
	/// <summary>获取服务的操作集合。</summary>
	public TOperations Operations { get; protected init; }
	#endregion
}
