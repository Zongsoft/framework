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

namespace Zongsoft.Data;

/// <summary>为数据服务的获取事件提供数据。</summary>
public class DataGettedEventArgs<T> : DataAccessEventArgs<DataSelectContextBase>
{
	#region 构造函数
	public DataGettedEventArgs(DataSelectContextBase context, T result) : base(context) => this.Result = result;
	#endregion

	#region 公共属性
	/// <summary>获取查询操作的首条数据。</summary>
	public T Result { get; }
	#endregion
}
