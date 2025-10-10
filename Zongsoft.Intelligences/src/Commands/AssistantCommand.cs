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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Intelligences.Commands;

public class AssistantCommand : CommandBase<CommandContext>, IServiceAccessor<IChatService>, IServiceAccessor<IModelService>
{
	#region 成员字段
	private IChatService _chatting;
	private IModelService _modeling;
	private IConnectionSettings _settings;
	#endregion

	#region 构造函数
	public AssistantCommand() : base("Assistant") { }
	public AssistantCommand(string name) : base(name) { }
	#endregion

	#region 公共属性
	public object Servicer { get; set; }
	public IConnectionSettings Settings
	{
		get => _settings;
		set
		{
			if(object.ReferenceEquals(_settings, value))
				return;

			var settings = _settings;
			if(settings != null)
			{
				_chatting?.Dispose();
				_chatting = null;

				if(_modeling is IDisposable disposable)
					disposable.Dispose();

				_modeling = null;
			}

			_settings = value;
		}
	}

	IChatService IServiceAccessor<IChatService>.Value
	{
		get
		{
			if(this.Servicer is IChatService service)
				return service;

			if(_chatting == null && _settings != null)
			{
				var factory = ApplicationContext.Current.Services.Resolves<IChatServiceFactory>(GetDriverName(_settings)).FirstOrDefault();

				if(factory != null)
					return _chatting ??= factory.Create(_settings);
			}

			return _chatting;
		}
	}

	IModelService IServiceAccessor<IModelService>.Value
	{
		get
		{
			if(this.Servicer is IModelService service)
				return service;

			if(_modeling == null && _settings != null)
			{
				var factory = ApplicationContext.Current.Services.Resolves<IModelServiceFactory>(GetDriverName(_settings)).FirstOrDefault();

				if(factory != null)
					return _modeling ??= factory.Create(_settings);
			}

			return _modeling;
		}
	}
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		switch(context.Value)
		{
			case string text:
				this.Settings = GetSettings(text);
				break;
			case Configuration.ConnectionSettings settings:
				this.Settings = settings;
				break;
			default:
				if(context.Value != null)
					this.Servicer = context.Value;
				break;
		}

		if(context.Arguments.Count > 0)
			this.Settings = GetSettings(context.Arguments[0]);

		return ValueTask.FromResult(this.Servicer ?? this.Settings);
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

	private static IConnectionSettings GetSettings(string text) => text != null && text.Contains('=') ?
		new ConnectionSettings(text) :
		ApplicationContext.Current.Configuration.GetConnectionSettings("AI/ConnectionSettings", text);
	#endregion
}
