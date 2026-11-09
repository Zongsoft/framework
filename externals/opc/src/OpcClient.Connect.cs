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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace Zongsoft.Externals.Opc;

partial class OpcClient
{
	public ValueTask ConnectAsync(string settings, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(settings))
			throw new ArgumentNullException(nameof(settings));

		return this.ConnectAsync(Configuration.OpcConnectionSettingsDriver.Instance.GetSettings(settings), cancellation);
	}

	public async ValueTask ConnectAsync(Configuration.OpcConnectionSettings settings, CancellationToken cancellation = default)
	{
		await _lifecycle.WaitAsync(cancellation);

		try
		{
			ObjectDisposedException.ThrowIf(_disposal != null, this);
			await this.ConnectCoreAsync(settings, cancellation);
		}
		finally { _lifecycle.Release(); }
	}

	private async ValueTask ConnectCoreAsync(Configuration.OpcConnectionSettings settings, CancellationToken cancellation)
	{
		if(settings == null || string.IsNullOrEmpty(settings.Server))
			throw new ArgumentNullException(nameof(settings));

		//验证客户端配置
		await _configuration.ValidateAsync(ApplicationType.Client, cancellation);

		var endpointConfiguration = EndpointConfiguration.Create(_configuration);
		var endpointDescription = await CoreClientUtils.SelectEndpointAsync(_configuration, settings.Server, false, 1000 * 10, null, cancellation);

		endpointDescription.SecurityMode = Utility.GetSecurityMode(settings.SecurityMode);
		if(endpointDescription.SecurityMode == MessageSecurityMode.Sign || endpointDescription.SecurityMode == MessageSecurityMode.SignAndEncrypt)
		{
			endpointDescription.SecurityPolicyUri = $"http://opcfoundation.org/UA/SecurityPolicy#{(string.IsNullOrEmpty(settings.SecurityPolicy) ? "Basic256Sha256" : settings.SecurityPolicy)}";

			var instance = new ApplicationInstance(_configuration, null)
			{
				ApplicationName = _configuration.ApplicationName,
				ApplicationType = _configuration.ApplicationType,
			};

			//自动生成客户端证书文件
			await instance.CheckApplicationInstanceCertificatesAsync(false, CertificateFactory.DefaultLifeTime, cancellation);
		}

		var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

		var name = string.IsNullOrEmpty(settings.Client) ?
			string.IsNullOrEmpty(settings.Instance) ? $"{this.Name}#{Common.Randomizer.GenerateString()}" : $"{this.Name}.{settings.Instance}" :
			string.IsNullOrEmpty(settings.Instance) ? settings.Client : $"{settings.Client}:{settings.Instance}";

		var locales = string.IsNullOrEmpty(settings.Locales) ? ["en"] : settings.Locales.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var identity = await settings.GetIdentityAsync(cancellation);

		//断开原有会话
		await this.DisconnectCoreAsync(cancellation);

		//创建新的会话
		var session = await _sessionFactory.CreateAsync(
			_configuration,
			endpoint,
			false,
			false,
			name,
			(uint)settings.Timeout.TotalMilliseconds,
			identity,
			locales,
			cancellation);

		try
		{
			//保存当前连接设置
			_settings = settings;

			//构建会话状态信息
			_state = new OpcClientState(session);

			//设置会话相关信息
			session.DeleteSubscriptionsOnClose = true;
			session.KeepAliveInterval = (int)settings.Heartbeat.TotalMilliseconds;
			session.KeepAlive += this.Session_KeepAlive;

			if(!session.Connected)
				await session.OpenAsync(name, identity, cancellation);

			//置空命名空间集
			_namespaces = null;

			//保存当前为新建会话
			_session = session;
		}
		catch
		{
			session.KeepAlive -= this.Session_KeepAlive;
			await session.DisposeAsync();
			throw;
		}
	}

	public async ValueTask ReconnectAsync(CancellationToken cancellation = default)
	{
		await _lifecycle.WaitAsync(cancellation);

		try
		{
			ObjectDisposedException.ThrowIf(_disposal != null, this);
			if(_session != null)
				await _session.ReconnectAsync(cancellation);
		}
		finally { _lifecycle.Release(); }
	}

	public async ValueTask DisconnectAsync(CancellationToken cancellation = default)
	{
		await _lifecycle.WaitAsync(cancellation);

		try { await this.DisconnectCoreAsync(cancellation); }
		finally { _lifecycle.Release(); }
	}

	private async ValueTask DisconnectCoreAsync(CancellationToken cancellation)
	{
		var session = Interlocked.Exchange(ref _session, null);
		if(session == null)
			return;

		List<Exception> errors = null;

		foreach(var subscriber in _subscribers)
		{
			try { await subscriber.DisposeAsync(); }
			catch(Exception exception) { (errors ??= []).Add(exception); }
		}

		session.KeepAlive -= this.Session_KeepAlive;

		try
		{
			if(session.Connected)
			{
				var status = await session.CloseAsync(cancellation);
				if(StatusCode.IsBad(status))
					throw new ServiceResultException(status);
			}
		}
		catch(Exception exception) { (errors ??= []).Add(exception); }

		try { await session.DisposeAsync(); }
		catch(Exception exception) { (errors ??= []).Add(exception); }

		_namespaces = null;
		_state = null;

		if(errors != null)
			throw new AggregateException(errors);
	}
}
