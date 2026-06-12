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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Upgrading;

public static class ReleaseExtension
{
	public static Release ToRelease(this Models.Release model)
	{
		var edition = string.IsNullOrEmpty(model.Edition) || model.Edition == "_" ? null : model.Edition;
		var release = new Release(model.Name, edition, model.Version, model.Platform, model.Architecture)
		{
			Kind = model.Kind,
			Size = model.Size,
			Title = model.Title,
			Summary = model.Summary,
			Creation = model.Creation,
			Description = model.Description,
			Deprecated = model.Deprecated || !model.Visible,
			Checksum = Common.Checksum.TryParse(model.Checksum, out var checksum) ? checksum : default,
		};

		release.Properties[nameof(model.ReleaseId)] = model.ReleaseId;

		if(!string.IsNullOrEmpty(model.Path))
			release.Path = IO.FileSystem.GetUrl(model.Path);

		if(!string.IsNullOrEmpty(model.Tags))
			release.Tags = model.Tags.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		if(model.Executors != null && model.Executors.Count > 0)
		{
			foreach(var executor in model.Executors)
				release.Executors.Add(new(executor.Event, executor.Command));
		}

		if(model.Properties != null && model.Properties.Count > 0)
		{
			foreach(var property in model.Properties)
				release.Properties.Add(new(property.Name, property.Value));
		}

		return release;
	}
}
