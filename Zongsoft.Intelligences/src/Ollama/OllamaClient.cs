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
		this.Settings = new Configuration.ConnectionSettings("ollama", $"server={client.Uri};model={client.SelectedModel}");
		this.Sessions = new ChatSessionManager(this);
	}

	internal OllamaClient(string url, string model = null)
	{
		_client = new OllamaApiClient(url, model);
		this.Settings = new Configuration.ConnectionSettings("ollama", $"server={url};model={model}");
		this.Sessions = new ChatSessionManager(this);
	}

	internal OllamaClient(Configuration.IConnectionSettings settings)
	{
		if(settings == null)
			throw new ArgumentNullException(nameof(settings));

		if(string.IsNullOrEmpty(settings["server"]))
			throw new ArgumentException($"The specified connection settings are missing the required server option.");

		_client = new OllamaApiClient(settings["server"], settings["model"]);
		this.Settings = settings;
		this.Sessions = new ChatSessionManager(this);
	}
	#endregion

	#region 公共属性
	public Configuration.IConnectionSettings Settings { get; }
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
