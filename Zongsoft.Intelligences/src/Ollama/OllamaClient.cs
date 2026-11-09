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

using OllamaSharp;

namespace Zongsoft.Intelligences.Ollama;

public partial class OllamaClient : IDisposable
{
	#region 成员字段
	private readonly IOllamaApiClient _client;
	#endregion

	#region 构造函数
	internal OllamaClient(IOllamaApiClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
		this.Settings = OllamaConnectionSettingsDriver.Instance.GetSettings($"server={client.Uri};model={client.SelectedModel}");
		this.Sessions = new ChatSessionManager(this);
	}

	internal OllamaClient(string url, string model = null)
	{
		_client = new OllamaApiClient(url, model);
		this.Settings = OllamaConnectionSettingsDriver.Instance.GetSettings($"server={url};model={model}");
		this.Sessions = new ChatSessionManager(this);
	}

	internal OllamaClient(OllamaConnectionSettings settings)
	{
		if(settings == null)
			throw new ArgumentNullException(nameof(settings));

		if(string.IsNullOrEmpty(settings.Server))
			throw new ArgumentException(Properties.Resources.Connection_ServerRequired_Message);

		_client = new OllamaApiClient(settings.Server, settings.Model);
		this.Settings = settings;
		this.Sessions = new ChatSessionManager(this);
	}
	#endregion

	#region 公共属性
	public OllamaConnectionSettings Settings { get; }
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(_client is IDisposable disposable)
			disposable.Dispose();
	}
	#endregion
}
