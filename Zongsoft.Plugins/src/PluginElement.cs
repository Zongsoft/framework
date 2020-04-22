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
using System.ComponentModel;

namespace Zongsoft.Plugins
{
	public abstract class PluginElement : INotifyPropertyChanged
	{
		#region 事件定义
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region 成员字段
		private string _name;
		private Plugin _plugin;
		#endregion

		#region 构造函数
		protected PluginElement(string name) : this(name, null)
		{
		}

		protected PluginElement(string name, Plugin plugin)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");

			this.Name = name;
			_plugin = plugin;
		}

		internal PluginElement(string name, bool ignoreNameValidation)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");

			if(ignoreNameValidation)
				_name = name;
			else
				this.Name = name;
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				return _name;
			}
			private set
			{
				if(string.IsNullOrEmpty(value))
					throw new ArgumentNullException();

				for(int i = 0; i < value.Length; i++)
				{
					var chr = value[i];

					if((chr == '_' || chr == '-' || chr == '$') ||
					   (chr >= '0' && chr <= '9') ||
					   (chr >= 'a' && chr <= 'z') ||
					   (chr >= 'A' && chr <= 'Z'))
						continue;

					throw new ArgumentException(string.Format("The plugin element name({0}) contains illegal characters.", value));
				}

				if(string.Equals(_name, value, StringComparison.Ordinal))
					return;

				_name = value;

				//激发“PropertyChanged”事件
				this.OnPropertyChanged("Name");
			}
		}

		public Plugin Plugin
		{
			get
			{
				return _plugin;
			}
			protected set
			{
				if(object.ReferenceEquals(_plugin, value))
					return;

				_plugin = value;

				//激发“PropertyChanged”事件
				this.OnPropertyChanged("Plugin");
			}
		}
		#endregion

		#region 保护方法
		protected void OnPropertyChanged(string propertyName)
		{
			this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			this.PropertyChanged?.Invoke(this, e);
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			var plugin = _plugin;

			if(plugin == null)
				return string.Format("{0}[{1}]", _name, this.GetType().Name);
			else
				return string.Format("{0}[{1}]@{2}", _name, this.GetType().Name, _plugin.Name);
		}
		#endregion
	}
}
