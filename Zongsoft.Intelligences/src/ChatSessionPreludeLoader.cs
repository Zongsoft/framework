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
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Intelligences;

[Service<IChatSessionLifetime>]
public sealed class ChatSessionPreludeLoader : IChatSessionLifetime
{
	public void OnCreated(IChatService service, IChatSession session)
	{
		if(string.IsNullOrEmpty(service.Settings.Name))
			return;

		//定义开场白目录位置
		var directory = Path.Combine(Path.GetDirectoryName($"{typeof(ChatSessionPreludeLoader).Assembly.Location}"), "preludes");

		if(Directory.Exists(directory))
		{
			var files = Enumerable.Concat(
				Directory.EnumerateFiles(directory, $"{service.Settings.Name}.txt"),
				Directory.EnumerateFiles(directory, $"{service.Settings.Name}-*.txt")
			).OrderBy(p => p).ToArray();

			var preludes = new List<string>(files.Length);

			foreach(var file in files)
			{
				using var reader = File.OpenText(file);
				var prelude = reader.ReadToEnd();

				if(!string.IsNullOrWhiteSpace(prelude))
					preludes.Add(prelude);
			}

			session.Options.Preludes = [.. preludes];
		}
	}

	void IChatSessionLifetime.OnActivated(IChatService service, IChatSession session) { }
	void IChatSessionLifetime.OnAbandoned(IChatService service, IChatSession session) { }
}
