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

namespace Zongsoft.Intelligences.Ollama;

[Services.Service<IModelService>(Tags = "Ollama")]
partial class OllamaClient : IModelService
{
	#region 公共属性
	public string Model
	{
		get => _client.SelectedModel;
		set => _client.SelectedModel = value;
	}
	#endregion

	#region 显式属性
	Configuration.IConnectionSettings IModelService.Settings => this.Settings;
	#endregion

	#region 公共方法
	public ValueTask<bool> ActivateAsync(string identifier, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(identifier))
			return ValueTask.FromResult(false);

		_client.SelectedModel = identifier;
		return ValueTask.FromResult(true);
	}

	public async ValueTask<IModel> GetModelAsync(string identifier, CancellationToken cancellation = default)
	{
		try
		{
			var response = await _client.ShowModelAsync(new OllamaSharp.Models.ShowModelRequest() { Model = identifier }, cancellation);
			if(response == null || response.Info == null)
				return null;

			return response.ToModel();
		}
		catch(System.Net.Http.HttpRequestException ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	public async IAsyncEnumerable<IModel> GetModelsAsync(string pattern, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
	{
		if(string.Equals(pattern, "running", StringComparison.OrdinalIgnoreCase))
		{
			var models = await _client.ListRunningModelsAsync(cancellation);

			foreach(var model in models)
				yield return model.ToModel();
		}
		else
		{
			var models = await _client.ListLocalModelsAsync(cancellation);

			foreach(var model in models)
				yield return model.ToModel();
		}
	}

	ValueTask<bool> IModelService.RunAsync(string identifier, CancellationToken cancellation) => throw new NotSupportedException();
	ValueTask<bool> IModelService.StopAsync(string identifier, CancellationToken cancellation) => throw new NotSupportedException();
	#endregion
}
