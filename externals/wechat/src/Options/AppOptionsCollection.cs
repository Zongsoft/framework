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

public class AppOptionsCollection : KeyedCollection<string, AppOptions>
{
	#region 公共属性
	public string Default { get; set; }
	#endregion

	#region 构造函数
	public AppOptionsCollection() : base(StringComparer.OrdinalIgnoreCase) { }
	#endregion

	#region 公共方法
	public AppOptions GetDefault() => this.Default != null && this.TryGetValue(this.Default, out var app) ? app : (this.Count > 0 ? this[0] : null);
	public bool TryGetDefault(out AppOptions result)
	{
		result = this.Default != null && this.TryGetValue(this.Default, out var app) ? app : (this.Count > 0 ? this[0] : null);
		return result != null;
	}

	public bool TryGetOption(string key, out AppOptions result)
	{
		if(string.IsNullOrEmpty(key))
		{
			key = this.Default;

			if(string.IsNullOrEmpty(key))
			{
				result = null;
				return false;
			}
		}

		return this.TryGetValue(key, out result);
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(AppOptions item) => item.Name;
	#endregion
}
