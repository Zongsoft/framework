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
using Zongsoft.Collections;

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

		//实例化当前访问的客户端实例对象
		await InstantiateAsync(parameters, cancellation);

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

	public static async ValueTask TraceAsync(string phase, string message, IDictionary<string, string> properties, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(phase) || properties == null || properties.Count == 0)
			return;

		if(!properties.TryGetValue("Fingerprint", out var fingerprint) || string.IsNullOrEmpty(fingerprint))
			return;

		var instance = await Module.Current.Accessor.SelectAsync<Models.Instance>(
			Condition.Equal(nameof(Models.Instance.InstanceCode), fingerprint),
			Paging.Limit(1),
			cancellation).FirstOrDefault(cancellation);

		if(instance == null)
			return;

		if(!properties.TryGetValue("name", out var name) || string.IsNullOrEmpty(name))
			return;
		if(!properties.TryGetValue("version", out var value) || string.IsNullOrEmpty(value) || !Versioning.Version.Number.TryParse(value, out var version))
			return;
		if(!properties.TryGetValue("platform", out value) || string.IsNullOrEmpty(value) || !Enum.TryParse<Platform>(value, true, out var platform))
			return;
		if(!properties.TryGetValue("architecture", out value) || string.IsNullOrEmpty(value) || !Enum.TryParse<Architecture>(value, true, out var architecture))
			return;

		var criteria = Condition.Equal(nameof(Models.Release.Name), name) &
			Condition.Equal(nameof(Models.Release.Version), (ulong)version) &
			Condition.Equal(nameof(Models.Release.Platform), platform) &
			Condition.Equal(nameof(Models.Release.Architecture), architecture) &
			Condition.Equal(nameof(Models.Release.Visible), true);

		if(properties.TryGetValue("edition", out value) && !string.IsNullOrEmpty(value) && value != "_")
			criteria.Add(Condition.Equal(nameof(Models.Release.Edition), value));
		else
			criteria.Add(Condition.In(nameof(Models.Release.Edition), [null, string.Empty, "_"]));

		var release = await Module.Current.Accessor.SelectAsync<Models.Release>(criteria, Paging.Limit(1), cancellation).FirstOrDefault(cancellation);

		if(release == null)
			return;

		var tracing = Model.Build<Models.ReleaseTracing>(tracing =>
		{
			tracing.ReleaseId = release.ReleaseId;
			tracing.InstanceId = instance.InstanceId;
			tracing.Phase = phase;
			tracing.Timestamp = DateTime.Now;
			tracing.Message = string.IsNullOrWhiteSpace(message) ? null : message;
			tracing.Description = properties.TryGetValue(nameof(tracing.Description), out var description) ? description : null;
		});

		await Module.Current.Accessor.UpsertAsync(tracing, cancellation);
	}

	private static async ValueTask<Models.Instance> InstantiateAsync(IDictionary<string, string> properties, CancellationToken cancellation = default)
	{
		if(properties == null || properties.Count == 0)
			return null;

		if(!properties.TryGetValue("Fingerprint", out var fingerprint) || string.IsNullOrEmpty(fingerprint))
			return null;

		var instance = await Module.Current.Accessor.SelectAsync<Models.Instance>(
			Condition.Equal(nameof(Models.Instance.InstanceCode), fingerprint),
			Paging.Limit(1),
			cancellation).FirstOrDefault(cancellation);

		if(instance == null)
		{
			instance = Model.Build<Models.Instance>(model =>
			{
				model.InstanceCode = fingerprint;
			});

			if(properties.TryGetValue(nameof(Environment.MachineName), out var value) && !string.IsNullOrEmpty(value))
				instance.Name = value;

			await Module.Current.Accessor.InsertAsync(instance, DataInsertOptions.IgnoreConstraint(), cancellation);
		}

		return instance;
	}
}
