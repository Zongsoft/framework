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

internal static class OllamaUtility
{
	public static IModel ToModel(this OllamaSharp.Models.Model model)
	{
		if(model == null)
			return null;

		var creation = model.ModifiedAt.Year > 2000 ? model.ModifiedAt : DateTimeOffset.MinValue;
		return new Model(model.Name, model.Name, model.Size, creation, model.ToString());
	}

	public static IModel ToModel(this OllamaSharp.Models.ShowModelResponse response)
	{
		if(response == null)
			return null;

		string name;
		name = response.Details.Family;

		if(string.IsNullOrEmpty(name) && response.Info.ExtraInfo.TryGetValue("general.basename", out var value))
			name = value as string;

		if(response.Info.ExtraInfo.TryGetValue("general.size_label", out value))
			name += $":{value}";

		return new Model(name, name, 0, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
	}
}
