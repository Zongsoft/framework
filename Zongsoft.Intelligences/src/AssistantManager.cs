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
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Intelligences;

[Service<IAssistantProvider>(Members = nameof(Default))]
public static class AssistantManager
{
	#region 单例字段
	public static readonly IAssistantProvider Default = new DefaultAssistantProvider();
	#endregion

	#region 静态方法
	public static IAssistant GetAssistant(string name = null)
	{
		var providers = ApplicationContext.Current.Services.ResolveAll<IAssistantProvider>();

		foreach(var provider in providers)
		{
			var assistant = provider.GetAssistant(name);

			if(assistant != null)
				return assistant;
		}

		return null;
	}

	public static IEnumerable<IAssistant> GetAssistants()
	{
		var providers = ApplicationContext.Current.Services.ResolveAll<IAssistantProvider>();

		foreach(var provider in providers)
		{
			foreach(var assistant in provider.GetAssistants())
				yield return assistant;
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class DefaultAssistantProvider : AssistantProviderBase<IConnectionSettings>
	{
		#region 重写方法
		protected override IEnumerable<IConnectionSettings> GetSettings() => ApplicationContext.Current.Configuration.GetOption<ConnectionSettingsCollection>("AI/ConnectionSettings");
		protected override IConnectionSettings GetSetting(string name) => ApplicationContext.Current.Configuration.GetConnectionSettings("AI/ConnectionSettings", name);
		protected override (IAssistant, IChangeToken) Create(IConnectionSettings setting)
		{
			var driver = GetDriverName(setting);
			var chattingFactory = ApplicationContext.Current.Services.ResolveTags<IChatServiceFactory>(driver).FirstOrDefault();
			var modelingFactory = ApplicationContext.Current.Services.ResolveTags<IModelServiceFactory>(driver).FirstOrDefault();

			return (new Assistant(setting.Name, driver)
			{
				Chatting = chattingFactory?.Create(setting),
				Modeling = modelingFactory?.Create(setting)
			}, null);
		}
		#endregion

		#region 私有方法
		private static string GetDriverName(IConnectionSettings settings)
		{
			if(settings == null)
				return null;

			return string.IsNullOrEmpty(settings.Driver?.Name) && settings.HasProperties ?
			settings.Properties["driver"] : settings.Driver?.Name;
		}
		#endregion
	}
	#endregion
}
