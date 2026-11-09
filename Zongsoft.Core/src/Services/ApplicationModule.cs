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
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Services;

public class ApplicationModule : IApplicationModule, IMatchable, IDisposable
{
	#region 成员字段
	private ServiceProvider _services;
	private readonly object _locker = new();
	#endregion

	#region 构造函数
	protected ApplicationModule(string name, Assembly assembly = null)
	{
		this.Name = name == null ? string.Empty : name.Trim();
		this.Properties = new Parameters();
		this.Assembly = assembly ?? this.GetType().Assembly;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Edition => field ??= this.GetEdition();
	public System.Version Version => field ??= this.GetVersion();

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public Assembly Assembly { get; }
	public Parameters Properties { get; }
	public string Title => this.GetTitle();
	public string Description => this.GetDescription();

	public virtual IServiceProvider Services
	{
		get
		{
			if(_services == null)
			{
				lock(_locker)
					_services ??= new ServiceProvider(this.Name, ApplicationContext.Current.Services.CreateScope().ServiceProvider);
			}

			return _services;
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual string GetEdition() => ApplicationIdentifier.Load(this).Edition;
	protected virtual System.Version GetVersion() => ApplicationIdentifier.Load(this).Version;
	protected virtual string GetTitle() => ApplicationModuleUtility.GetTitle(this);
	protected virtual string GetDescription() => ApplicationModuleUtility.GetDescription(this);
	#endregion

	#region 匹配方法
	bool IMatchable.Match(object parameter) => parameter != null && string.Equals(this.Name, parameter.ToString(), StringComparison.OrdinalIgnoreCase);
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing) { }
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name}@{this.Version}";
	#endregion
}

public class ApplicationModule<TEvents> : ApplicationModule where TEvents : EventRegistryBase, new()
{
	#region 构造函数
	protected ApplicationModule(string name, Assembly assembly = null) : base(name, assembly) => this.Events = new TEvents();
	#endregion

	#region 公共属性
	/// <summary>获取本模块的事件注册表。</summary>
	public TEvents Events { get; }
	#endregion
}
