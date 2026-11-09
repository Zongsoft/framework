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
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Wechat.Options;

public class AppletOptionsCollection : KeyedCollection<string, AppletOptions>
{
	#region 公共属性
	public string Default { get; set; }
	#endregion

	#region 构造函数
	public AppletOptionsCollection() : base(StringComparer.OrdinalIgnoreCase) { }
	#endregion

	#region 公共方法
	public AppletOptions GetDefault() => this.Default != null && this.TryGetValue(this.Default, out var applet) ? applet : (this.Count > 0 ? this[0] : null);
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(AppletOptions item) => item.Name;
	#endregion
}
