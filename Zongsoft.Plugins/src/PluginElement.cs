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
using System.ComponentModel;

namespace Zongsoft.Plugins;

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
	protected PluginElement(string name) : this(name, null) { }
	protected PluginElement(string name, Plugin plugin)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		_plugin = plugin;
	}

	internal PluginElement(string name, bool ignoreNameValidation)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(ignoreNameValidation)
			_name = name;
		else
			this.Name = name;
	}
	#endregion

	#region 公共属性
	public string Name
	{
		get => _name;
		private set
		{
			if(string.IsNullOrEmpty(value))
				throw new ArgumentNullException();

			for(int i = 0; i < value.Length; i++)
			{
				var chr = value[i];

				if((chr == '_' || chr == '-' || chr == '.' || chr == '$') ||
				   (chr >= '0' && chr <= '9') ||
				   (chr >= 'a' && chr <= 'z') ||
				   (chr >= 'A' && chr <= 'Z'))
					continue;

				throw new ArgumentException(string.Format(Properties.Resources.Plugin_ElementNameInvalid_Message, value));
			}

			if(string.Equals(_name, value, StringComparison.Ordinal))
				return;

			_name = value;

			//激发“PropertyChanged”事件
			this.OnPropertyChanged(nameof(this.Name));
		}
	}

	public Plugin Plugin
	{
		get => _plugin;
		protected set
		{
			if(object.ReferenceEquals(_plugin, value))
				return;

			_plugin = value;

			//激发“PropertyChanged”事件
			this.OnPropertyChanged(nameof(this.Plugin));
		}
	}
	#endregion

	#region 保护方法
	protected void OnPropertyChanged(string propertyName) => this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
	protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => this.PropertyChanged?.Invoke(this, e);
	#endregion

	#region 重写方法
	public override string ToString() => _plugin == null ? $"{_name}[{this.GetType().Name}]" : $"{_name}[{this.GetType().Name}]@{_plugin}";
	#endregion
}
