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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;

namespace Zongsoft.Upgrading;

public static class Upgrader
{
	public static async IAsyncEnumerable<Release> GetAsync(string name, string edition, Platform platform, Architecture architecture, IDictionary<string, string> parameters, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		const string SCHEMA = $"*, {nameof(Models.Release.Executors)}{{*}}, {nameof(Models.Release.Properties)}{{*}}";

		var criteria = ConditionCollection.And(
			Condition.Equal(nameof(Models.Release.Name), name),
			Condition.Equal(nameof(Models.Release.Visible), true),
			Condition.Equal(nameof(Models.Release.Published), true),
			Condition.Equal(nameof(Models.Release.Deprecated), false),
			Condition.Equal(nameof(Models.Release.Platform), platform),
			Condition.Equal(nameof(Models.Release.Architecture), architecture),
			Condition.NotEqual(nameof(Models.Release.Path), null),
			Condition.GreaterThan(nameof(Models.Release.Size), 0)
		);

		if(string.IsNullOrWhiteSpace(edition) || edition == "_")
			criteria.Add(Condition.In(nameof(Models.Release.Edition), string.Empty, "_"));
		else
			criteria.Add(Condition.Equal(nameof(Models.Release.Edition), edition));

		var currentlyVersion = parameters.TryGetValue("CurrentlyVersion", out var value) && Versioning.Version.Number.TryParse(value, out var version) ? version : default;
		if(!currentlyVersion.IsZero)
			criteria.Add(Condition.GreaterThan(nameof(Models.Release.Version), (ulong)currentlyVersion));

		var upgradingVersion = parameters.TryGetValue("UpgradingVersion", out value) && Versioning.Version.Number.TryParse(value, out version) ? version : default;
		if(!upgradingVersion.IsZero)
			criteria.Add(Condition.LessThanEqual(nameof(Models.Release.Version), (ulong)upgradingVersion));

		await foreach(var model in Module.Current.Accessor.SelectAsync<Models.Release>(criteria, SCHEMA, cancellation))
		{
			if(!await ExistsAsync(model.Path, cancellation))
				break;

			//如果发布模型指定了评估器名称，并且在模块的评估器集合中找到了对应的评估器，则使用该评估器对当前发布进行评估
			if(model.EvaluatorName != null && Module.Current.Evaluators.TryGetValue(model.EvaluatorName, out var evaluator))
			{
				//如果评估器评估不通过，则跳过当前发布
				if(!await evaluator.EvaluateAsync(name, model.EvaluatorSetting, parameters, cancellation))
					continue;
			}

			yield return model.ToRelease();
		}

		static ValueTask<bool> ExistsAsync(string path, CancellationToken cancellation)
		{
			if(string.IsNullOrWhiteSpace(path))
				return ValueTask.FromResult(false);

			try
			{
				return IO.FileSystem.File.ExistsAsync(path, cancellation);
			}
			catch { return ValueTask.FromResult(false); }
		}
	}

	internal static Release ToRelease(this Models.Release model)
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
			Checksum = Checksum.TryParse(model.Checksum, out var checksum) ? checksum : default,
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

	public static async ValueTask TraceAsync(string id, string phase, string message, IDictionary<string, string> properties, CancellationToken cancellation = default)
	{
	}
}
