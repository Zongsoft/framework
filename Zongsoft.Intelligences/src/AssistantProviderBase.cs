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

using Microsoft.Extensions.Primitives;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Intelligences;

public abstract class AssistantProviderBase<TSetting> : IAssistantProvider, IServiceProvider<IAssistant> where TSetting : IConnectionSettings
{
	#region 成员字段
	private readonly MemoryCache _cache = new();
	#endregion

	#region 公共方法
	public IAssistant GetAssistant(string name = null)
	{
		return _cache.GetOrCreate(name ?? string.Empty, key =>
		{
			var setting = this.GetSetting((string)key);
			if(setting is null)
				return (null, Notification.Notified);

			return this.Create(setting);
		});
	}

	public IEnumerable<IAssistant> GetAssistants()
	{
		var settings = this.GetSettings();

		foreach(var setting in settings)
		{
			if(setting is null)
				continue;

			var result = _cache.GetOrCreate(setting.Name ?? string.Empty, key =>
			{
				return this.Create(setting);
			});

			if(result != null)
				yield return result;
		}
	}
	#endregion

	#region 显式实现
	IAssistant IServiceProvider<IAssistant>.GetService(string name) => this.GetAssistant(name);
	#endregion

	#region 抽象方法
	protected abstract TSetting GetSetting(string name);
	protected abstract IEnumerable<TSetting> GetSettings();
	protected abstract (IAssistant, IChangeToken) Create(TSetting setting);
	#endregion
}
