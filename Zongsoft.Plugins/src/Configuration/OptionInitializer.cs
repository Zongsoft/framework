/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using Zongsoft.Plugins;
using Zongsoft.Services;

namespace Zongsoft.Configuration.Plugins
{
	public class OptionInitializer : Zongsoft.Services.IApplicationFilter, BackgroundService
	{
		public OptionInitializer()
		{
		}

		#region 公共属性
		public string Name
		{
			get => nameof(OptionInitializer);
		}
		#endregion

		#region 初始化器
		public void Initialize(PluginApplicationContext context)
		{
			if(context == null)
				return;

			var builder = new ConfigurationBuilder()
				.AddConfiguration(context.Configuration, true)
				.AddOptionFile("");

			ApplicationContext.Current.Configuration = builder.Build();

			//将当前应用的主配置文件加入到选项管理器中
			if(context.Configuration != null)
				OptionManager.Instance.Providers.Add(context.Configuration);

			context.PluginContext.PluginTree.Loader.PluginLoaded += Loader_PluginLoaded;
			context.PluginContext.PluginTree.Loader.PluginUnloaded += Loader_PluginUnloaded;
		}

		void Zongsoft.Services.IApplicationFilter.Initialize(Zongsoft.Services.IApplicationContext context)
		{
			this.Initialize(context as PluginApplicationContext);
		}
		#endregion

		#region 事件处理
		private void Loader_PluginLoaded(object sender, PluginLoadedEventArgs e)
		{
			if(OptionUtility.HasConfigurationFile(e.Plugin))
			{
				var proxy = new ConfigurationProxy(() => OptionUtility.GetConfiguration(e.Plugin));
				OptionManager.Instance.Providers.Add(proxy);
			}
		}

		private void Loader_PluginUnloaded(object sender, PluginUnloadedEventArgs e)
		{
			var providers = OptionManager.Instance.Providers;

			var found = providers.FirstOrDefault(provider =>
			{
				var proxy = provider as ConfigurationProxy;

				return (proxy != null && proxy.IsValueCreated &&
				        string.Equals(proxy.Value.FilePath, OptionUtility.GetConfigurationFilePath(e.Plugin)));
			});

			if(found != null)
				providers.Remove(found);
		}
		#endregion

		#region 嵌套子类
		private class ConfigurationProxyLoader : Configuration.OptionConfigurationLoader
		{
			#region 构造函数
			public ConfigurationProxyLoader(OptionNode root) : base(root)
			{
			}
			#endregion

			public override void Load(IOptionProvider provider)
			{
				var proxy = provider as ConfigurationProxy;

				if(proxy != null)
					base.LoadConfiguration(proxy.Value);
				else
					base.Load(provider);
			}

			public override void Unload(IOptionProvider provider)
			{
				var proxy = provider as ConfigurationProxy;

				if(proxy != null)
					base.UnloadConfiguration(proxy.Value);
				else
					base.Unload(provider);
			}
		}

		[OptionLoader(LoaderType = typeof(ConfigurationProxyLoader))]
		private class ConfigurationProxy : IOptionProvider
		{
			#region 成员字段
			private readonly Lazy<Configuration.OptionConfiguration> _proxy;
			#endregion

			#region 构造函数
			public ConfigurationProxy(Func<Configuration.OptionConfiguration> valueFactory)
			{
				if(valueFactory == null)
					throw new ArgumentNullException("valueFactory");

				_proxy = new Lazy<Configuration.OptionConfiguration>(valueFactory, true);
			}
			#endregion

			#region 公共属性
			public Configuration.OptionConfiguration Value
			{
				get
				{
					return _proxy.Value;
				}
			}

			public bool IsValueCreated
			{
				get
				{
					return _proxy.IsValueCreated;
				}
			}
			#endregion

			#region 公共方法
			public object GetOptionValue(string text)
			{
				var configuration = _proxy.Value;

				if(configuration != null)
					return configuration.GetOptionValue(text);

				return null;
			}

			public void SetOptionValue(string text, object value)
			{
				var configuration = _proxy.Value;

				if(configuration != null)
					configuration.SetOptionValue(text, value);
			}
			#endregion
		}
		#endregion
	}
}
