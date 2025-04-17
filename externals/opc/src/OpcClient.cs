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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Externals.Opc;

public class OpcClient : IDisposable
{
	#region 成员字段
	private ApplicationConfiguration _configuration;
	private Session _session;
	#endregion

	#region 构造函数
	public OpcClient(string name = null)
	{
		this.Name = string.IsNullOrEmpty(name) ? "Opc Client" : name;

		_configuration = new ApplicationConfiguration()
		{
			ApplicationName = this.Name,
			ApplicationType = ApplicationType.Client,
		};
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	#endregion

	#region 公共方法
	public async ValueTask ConnectAsync(string url, CancellationToken cancellation = default)
	{
		var endpointDescription = CoreClientUtils.SelectEndpoint(_configuration, url, false, 1000 * 3);
		var endpointConfiguration = EndpointConfiguration.Create(_configuration);
		var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

		_session = await Session.Create(
			_configuration,
			endpoint,
			false,
			this.Name,
			1000,
			new UserIdentity(),
			null,
			cancellation);

		await _session.OpenAsync(this.Name, new UserIdentity(), cancellation);
	}

	public async ValueTask WriteAsync(string key, object value, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));

		var request = new RequestHeader()
		{
			Timestamp = DateTime.UtcNow,
		};

		var values = new WriteValueCollection();
		values.Add(new WriteValue()
		{
			NodeId = "ns=2;i=100",
			Value = new DataValue(new Variant(100)),
		});

		var result = await _session.WriteAsync(request, values, cancellation);
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(!_session.Disposed)
			_session.Dispose();

		_configuration = null;
	}
	#endregion
}
