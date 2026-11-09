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

public class AuthorityOptionsCollection : KeyedCollection<string, AuthorityOptions>
{
	#region 公共属性
	public string Default { get; set; }
	#endregion

	#region 构造函数
	public AuthorityOptionsCollection() : base(StringComparer.OrdinalIgnoreCase) { }
	#endregion

	#region 公共方法
	public AuthorityOptions GetDefault() => this.Default != null && this.TryGetValue(this.Default, out var authority) ? authority : (this.Count > 0 ? this[0] : null);
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(AuthorityOptions item) => item.Name;
	#endregion
}
