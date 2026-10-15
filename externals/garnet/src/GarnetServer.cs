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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Garnet library.
 *
 * The Zongsoft.Externals.Garnet is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Garnet is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Garnet library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Externals.Garnet.Configuration;

namespace Zongsoft.Externals.Garnet;

public class GarnetServer(string name = null) : Components.WorkerBase(name)
{
	#region 成员字段
	private global::Garnet.GarnetServer _server;
	#endregion

	#region 公共属性
	/// <summary>获取或设置服务器设置名称。只能在停止状态下修改。</summary>
	public string Setting
	{
		get => string.IsNullOrEmpty(field) ? this.Name : field;
		set
		{
			if(this.State != Components.WorkerState.Stopped)
				throw new InvalidOperationException(Properties.Resources.GarnetServer_SettingImmutable_Message);

			field = value;
		}
	}
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();

		var settings = GetSettings(this.Setting);
		global::Garnet.GarnetServer server = null;

		try
		{
			server = new([.. settings.GetArguments()]);
			cancellation.ThrowIfCancellationRequested();

			server.Start();

			cancellation.ThrowIfCancellationRequested();
			_server = server;
		}
		catch
		{
			server?.Dispose();
			throw;
		}

		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		Interlocked.Exchange(ref _server, null)?.Dispose();
		return Task.CompletedTask;
	}
	#endregion

	#region 私有方法
	private static GarnetConnectionSettings GetSettings(string name)
	{
		var settings = ApplicationContext.Current?.Configuration.GetOption<SettingsCollection>("/Externals/Garnet");

		if(settings != null && settings.TryGetValue(name ?? string.Empty, out var result))
			return GarnetConnectionSettingsDriver.Instance.GetSettings(result.Name, result.Value);

		throw new InvalidOperationException(string.Format(Properties.Resources.GarnetServer_SettingMissing_Message, name));
	}
	#endregion
}
