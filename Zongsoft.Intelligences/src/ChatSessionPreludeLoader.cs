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

			session.Options.Preludes = [..preludes];
		}
	}

	void IChatSessionLifetime.OnActivated(IChatService service, IChatSession session) { }
	void IChatSessionLifetime.OnAbandoned(IChatService service, IChatSession session) { }
}
