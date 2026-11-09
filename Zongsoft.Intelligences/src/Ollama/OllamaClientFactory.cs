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

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Intelligences.Ollama;

[Service<IChatServiceFactory, IModelServiceFactory>(Tags = "Ollama", Members = nameof(Instance))]
public class OllamaClientFactory : IChatServiceFactory, IModelServiceFactory
{
	#region 单例字段
	public static readonly OllamaClientFactory Instance = new();
	#endregion

	#region 创建方法
	public static OllamaClient Create(IConnectionSettings settings) => new(settings as OllamaConnectionSettings ?? throw new ArgumentException($""));
	IChatService IChatServiceFactory.Create(IConnectionSettings settings) => Create(settings);
	IModelService IModelServiceFactory.Create(IConnectionSettings settings) => Create(settings);
	#endregion
}
