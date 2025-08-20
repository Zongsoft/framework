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
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.AI;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Intelligences.Commands;

public class LlmCommand : CommandBase<CommandContext>, IServiceAccessor<IChatClient>, IServiceAccessor<IModelService>
{
	#region 成员字段
	private IChatClient _chatting;
	private IModelService _modeling;
	private IConnectionSettings _settings;
	#endregion

	#region 构造函数
	public LlmCommand() : base("LLM") { }
	public LlmCommand(string name) : base(name) { }
	#endregion

	#region 公共属性
	public object Client { get; set; }
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

	IChatClient IServiceAccessor<IChatClient>.Value
	{
		get
		{
			if(this.Client is IChatClient service)
				return service;

			if(_chatting == null && _settings != null)
			{
				var factory = ApplicationContext.Current.Services.Resolve<IChatClientFactory>(_settings.Driver.Name);

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
			if(this.Client is IModelService service)
				return service;

			if(_modeling == null && _settings != null)
			{
				var factory = ApplicationContext.Current.Services.Resolve<IModelServiceFactory>(_settings.Driver.Name);

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
			case IConnectionSettings settings:
				this.Settings = settings;
				break;
			default:
				if(context.Value != null)
					this.Client = context.Value;
				break;
		}

		if(context.Expression.Arguments.Count > 0)
			this.Settings = GetSettings(context.Expression.Arguments[0]);

		return ValueTask.FromResult(this.Client ?? this.Settings);
	}
	#endregion

	#region 私有方法
	private static IConnectionSettings GetSettings(string text)
	{
		if(string.IsNullOrEmpty(text))
			return ApplicationContext.Current.Configuration.GetConnectionSettings("AI/ConnectionSettings", null);

		return text.Contains('=') ?
			new ConnectionSettings(text) :
			ApplicationContext.Current.Configuration.GetConnectionSettings("AI/ConnectionSettings", text);
	}
	#endregion
}
