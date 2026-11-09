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

/// <summary>为数据访问的更新事件提供数据。</summary>
public class DataUpdatingEventArgs : DataUpdatedEventArgs
{
	#region 构造函数
	public DataUpdatingEventArgs(DataUpdateContextBase context) : base(context) { }
	#endregion

	#region 公共属性
	/// <summary>获取或设置一个值，表示是否取消当前操作。</summary>
	public bool Cancel { get; set; }
	#endregion
}
