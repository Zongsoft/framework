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
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Velopack;
using Velopack.Sources;

namespace Zongsoft.Externals.Velopack;

public partial class VelopackSourceFactory
{
	public static readonly IVelopackSourceFactory Web = new WebSourceFactory();
	public static readonly IVelopackSourceFactory File = new FileSourceFactory();
	public static readonly IVelopackSourceFactory Gitea = new GiteaSourceFactory();
	public static readonly IVelopackSourceFactory Github = new GithubSourceFactory();
	public static readonly IVelopackSourceFactory Gitlab = new GitlabSourceFactory();

	private sealed class WebSourceFactory() : VelopackSourceFactoryBase<SimpleWebSource>("Web")
	{
		protected override SimpleWebSource Create(string url, IReadOnlyDictionary<string, string> settings)
		{
			return new(url, null, Utility.GetTimeout(settings).TotalMinutes);
		}
	}

	private sealed class FileSourceFactory() : VelopackSourceFactoryBase<SimpleFileSource>("File")
	{
		protected override SimpleFileSource Create(string url, IReadOnlyDictionary<string, string> settings)
		{
			return new(new(url));
		}
	}

	private sealed class GiteaSourceFactory : VelopackSourceFactoryBase<GiteaSource>
	{
		protected override GiteaSource Create(string url, IReadOnlyDictionary<string, string> settings)
		{
			if(Utility.TryGetToken(settings, out var token))
				return new(url, token, true);

			throw new InvalidOperationException($"Missing the required access token of the Gitea.");
		}
	}

	private sealed class GithubSourceFactory : VelopackSourceFactoryBase<GithubSource>
	{
		protected override GithubSource Create(string url, IReadOnlyDictionary<string, string> settings)
		{
			if(Utility.TryGetToken(settings, out var token))
				return new(url, token, true);

			throw new InvalidOperationException($"Missing the required access token of the GitHub.");
		}
	}

	private sealed class GitlabSourceFactory : VelopackSourceFactoryBase<GitlabSource>
	{
		protected override GitlabSource Create(string url, IReadOnlyDictionary<string, string> settings)
		{
			if(Utility.TryGetToken(settings, out var token))
				return new(url, token, true);

			throw new InvalidOperationException($"Missing the required access token of the GitLab.");
		}
	}
}

partial class VelopackSourceFactory
{
	private static readonly Lazy<VelopackSourceFactoryCollection> _factories = new(() => [], true);

	public static VelopackSourceFactoryCollection Factories => _factories.Value;
	public static IUpdateSource Create(Configuration.VelopackConnectionSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);

		if(_factories.Value.TryGetValue(string.IsNullOrEmpty(settings.Source) ? nameof(Web) : settings.Source, out var factory))
			return factory.Create(string.IsNullOrEmpty(settings.Url) ? "http://localhost" : settings.Url, settings);

		throw new InvalidOperationException($"The specified '{settings.Source}' update source was not found.");
	}
}
