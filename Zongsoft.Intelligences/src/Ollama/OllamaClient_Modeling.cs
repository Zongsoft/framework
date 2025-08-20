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
using System.Collections.Generic;

namespace Zongsoft.Intelligences.Ollama;

[Services.Service<IModelService>(Tags = "Ollama")]
partial class OllamaClient : IModelService
{
	#region 公共方法
	public void Active(string model) => _client.SelectedModel = model;
	public async ValueTask<IModel> GetModelAsync(string identifier, CancellationToken cancellation = default)
	{
		var response = await _client.ShowModelAsync(new OllamaSharp.Models.ShowModelRequest() { Model = identifier }, cancellation);
		if(response == null || response.Info == null)
			return null;

		return new Model();
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
