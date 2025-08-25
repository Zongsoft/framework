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
 * Copyright (C) 2025-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences library.
 *
 * The Zongsoft.Intelligences is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences library. If not, see <http://www.gnu.org/licenses/>.
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
