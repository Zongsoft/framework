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
using System.Collections.Generic;

using Zongsoft.Options;
using Zongsoft.Options.Configuration;

namespace Zongsoft.Options.Plugins
{
	internal class PluginSettingsProvider : ISettingsProvider
	{
		#region 成员字段
		private Zongsoft.Plugins.Plugin _plugin;
		private OptionConfiguration _configuration;
		private ISettingsProvider _settings;
		#endregion

		#region 构造函数
		internal PluginSettingsProvider(Zongsoft.Plugins.Plugin plugin, OptionConfiguration configuration)
		{
			_plugin = plugin;
			_configuration = configuration;
			_settings = (ISettingsProvider)_configuration.GetOptionValue("/settings");
		}
		#endregion

		#region 公共属性
		public OptionConfiguration Configuration
		{
			get
			{
				return _configuration;
			}
		}
		#endregion

		#region 公共方法
		public object GetValue(string name)
		{
			if(_settings == null)
				return null;

			var value = _settings.GetValue(name);

			if(value != null)
				return value;

			return this.RecursiveGetValue(name);
		}

		public void SetValue(string name, object value)
		{
			if(_settings == null)
				throw new NotSupportedException();

			_settings.SetValue(name, value);
			_configuration.Save();
		}
		#endregion

		#region 私有方法
		private object RecursiveGetValue(string name)
		{
			object value;

			foreach(var dependency in _plugin.Manifest.Dependencies)
			{
				var provider = PluginSettingsProviderFactory.GetProvider(dependency.Plugin);

				if(provider != null && provider._settings != null)
				{
					value = provider._settings.GetValue(name);

					if(value != null)
						return value;
				}
			}

			if(_plugin.Parent != null)
				return PluginSettingsProviderFactory.GetProvider(_plugin.Parent).GetValue(name);

			return null;
		}
		#endregion
	}
}
