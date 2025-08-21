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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Intelligences;

[Service<ICopilotProvider>(Members = nameof(Default))]
public static class CopilotManager
{
	#region 单例字段
	public static readonly ICopilotProvider Default = new DefaultCopilotProvider();
	#endregion

	#region 静态方法
	public static ICopilot GetCopilot(string name = null)
	{
		var providers = ApplicationContext.Current.Services.ResolveAll<ICopilotProvider>();

		foreach(var provider in providers)
		{
			var copilot = provider.GetCopilot(name);

			if(copilot != null)
				return copilot;
		}

		return null;
	}
	#endregion

	private sealed class DefaultCopilotProvider : ICopilotProvider, IServiceProvider<ICopilot>
	{
		#region 公共方法
		public ICopilot GetCopilot(string name = null)
		{
			var settings = ApplicationContext.Current.Configuration.GetConnectionSettings("AI/ConnectionSettings", name);
			if(settings == null)
				return null;

			var driver = GetDriverName(settings);
			var chattingFactory = ApplicationContext.Current.Services.ResolveTags<IChatServiceFactory>(driver).FirstOrDefault();
			var modelingFactory = ApplicationContext.Current.Services.ResolveTags<IModelServiceFactory>(driver).FirstOrDefault();

			return new Copilot(settings.Name, driver)
			{
				Chatting = chattingFactory?.Create(settings),
				Modeling = modelingFactory?.Create(settings)
			};
		}

		public IEnumerable<ICopilot> GetCopilots()
		{
			var settings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingsCollection>("AI/ConnectionSettings");
			if(settings == null)
				yield break;

			foreach(var setting in settings)
			{
				var driver = GetDriverName(setting);
				var chattingFactory = ApplicationContext.Current.Services.ResolveTags<IChatServiceFactory>(driver).FirstOrDefault();
				var modelingFactory = ApplicationContext.Current.Services.ResolveTags<IModelServiceFactory>(driver).FirstOrDefault();

				yield return new Copilot(setting.Name, driver)
				{
					Chatting = chattingFactory?.Create(setting),
					Modeling = modelingFactory?.Create(setting)
				};
			}
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

		#region 显式实现
		ICopilot IServiceProvider<ICopilot>.GetService(string name) => this.GetCopilot(name);
		#endregion
	}
}
