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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Upgrading.Models;

namespace Zongsoft.Upgrading.Services;

/// <summary>表示发布的数据服务基类。</summary>
[DataService<ReleaseCriteria>]
public class ReleaseService(IServiceProvider serviceProvider, DataServiceMutability? mutability = null) : DataServiceBase<Models.Release>(serviceProvider, mutability ?? DataServiceMutability.All)
{
	#region 公共属性
	/// <summary>获取发布属性子服务。</summary>
	public PropertyService Properties => this.GetSubservice<PropertyService>();
	/// <summary>获取发布执行器子服务。</summary>
	public ExecutorService Executors => this.GetSubservice<ExecutorService>();
	/// <summary>获取发布跟踪子服务。</summary>
	public TracingService Tracings => this.GetSubservice<TracingService>();
	#endregion

	#region 公共方法
	public async ValueTask<bool> PublishAsync(uint releaseId, CancellationToken cancellation = default) => await this.UpdateAsync(new { Published = true }, Condition.Equal(nameof(Models.Release.ReleaseId), releaseId), null, cancellation) > 0;
	public async ValueTask<bool> DeprecateAsync(uint releaseId, CancellationToken cancellation = default) => await this.UpdateAsync(new { Deprecated = true }, Condition.Equal(nameof(Models.Release.ReleaseId), releaseId), null, cancellation) > 0;

	public async ValueTask<string> GetFilePathAsync(uint releaseId, CancellationToken cancellation = default)
	{
		if(releaseId == 0)
			throw new DataArgumentException(nameof(releaseId));

		if(await this.GetAsync(releaseId, null, cancellation) is not Models.Release release)
			return null;

		var path = Settings.Current?["storage"];
		if(string.IsNullOrEmpty(path))
			return null;

		var filename = string.IsNullOrWhiteSpace(release.Edition) ?
			$"{release.Name}@{release.Version}_{Application.GetRuntimeIdentifier(release.Platform, release.Architecture)}" :
			$"{release.Name}-{release.Edition}@{release.Version}_{Application.GetRuntimeIdentifier(release.Platform, release.Architecture)}";

		return Zongsoft.IO.Path.Combine(path, release.Name.ToLowerInvariant(), filename);
	}

	public async ValueTask<bool> SetFilePathAsync(uint releaseId, string value, long size, CancellationToken cancellation = default)
	{
		try
		{
			var checksum = await ChecksumAsync(value, cancellation);

			var count = await this.UpdateAsync(new
			{
				Path = value,
				Size = (ulong)size,
				Checksum = checksum,
				Modification = DateTime.Now,
			}, Condition.Equal(nameof(Models.Release.ReleaseId), releaseId), null, cancellation);

			if(count < 1)
				DeleteFile(value);

			return count > 0;
		}
		catch
		{
			DeleteFile(value);
			throw;
		}

		static void DeleteFile(string path)
		{
			if(string.IsNullOrEmpty(path))
				return;

			try
			{
				Zongsoft.IO.FileSystem.File.Delete(path);
			}
			catch { }
		}

		static async ValueTask<string> ChecksumAsync(string path, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(path))
				return null;

			using var stream = await Zongsoft.IO.FileSystem.File.OpenAsync(path, System.IO.FileMode.Open, cancellation);
			var checksum = await Zongsoft.Common.Checksum.ComputeAsync(null, stream, cancellation);
			return checksum.ToString();
		}
	}

	public async ValueTask<Models.Release> ImportAsync(Release release, CancellationToken cancellation = default)
	{
		if(release == null)
			throw new ArgumentNullException(nameof(release));

		var model = Model.Build<Models.Release>();

		model.Name = release.Name;
		model.Kind = release.Kind;
		model.Tags = release.Tags == null ? null : string.Join(',', release.Tags);
		model.Edition = string.IsNullOrEmpty(release.Edition) ? "_" : release.Edition;
		model.Version = release.Version;
		model.Platform = release.Platform;
		model.Architecture = release.Architecture;
		model.Title = release.Title;
		model.Summary = release.Summary;
		model.Description = release.Description;
		model.Deprecated = release.Deprecated;

		if(!release.Checksum.IsEmpty)
			model.Checksum = release.Checksum.ToString();

		if(release.Executors.Count > 0)
		{
			var executors = new List<ReleaseExecutor>(release.Executors.Count);

			foreach(var executor in release.Executors)
			{
				var executorModel = Model.Build<ReleaseExecutor>();
				executorModel.Event = executor.Event;
				executorModel.Command = executor.Command;
				executors.Add(executorModel);
			}

			model.Executors = executors;
		}

		if(release.Properties.Count > 0)
		{
			var properties = new List<ReleaseProperty>(release.Properties.Count);

			foreach(var property in release.Properties)
			{
				var propertyModel = Model.Build<ReleaseProperty>();
				propertyModel.Name = property.Key;
				propertyModel.Value = Common.Convert.ConvertValue(property.Value, (string)null);
				properties.Add(propertyModel);
			}

			model.Properties = properties;
		}

		return await this.DataAccess.InsertAsync(model, $"*,{nameof(Models.Release.Executors)}{{*}},{nameof(Models.Release.Properties)}{{*}}", cancellation) > 0 ? model : null;
	}
	#endregion

	#region 重写方法
	protected override void OnValidate(DataServiceMethod method, ISchema schema, IDataDictionary<Models.Release> data, IDataMutateOptions options)
	{
		if(data.TryGetValue(nameof(Models.Release.Edition), out string edition) && string.IsNullOrWhiteSpace(edition))
			data.SetValue(nameof(Models.Release.Edition), "_");

		base.OnValidate(method, schema, data, options);
	}

	protected override void OnInserted(DataInsertContextBase context)
	{
		if(context.Count > 0)
		{
			if(context.IsMultiple)
			{
				foreach(var dictionary in DataDictionary.GetDictionaries<Models.Release>((System.Collections.IEnumerable)context.Data))
					this.Synchronize(dictionary);
			}
			else
			{
				this.Synchronize(DataDictionary.GetDictionary<Models.Release>(context.Data));
			}
		}

		base.OnInserted(context);
	}
	#endregion

	#region 虚拟方法
	protected virtual void Synchronize(IDataDictionary<Models.Release> dictionary)
	{
		if(dictionary == null || dictionary.IsEmpty)
			return;

		if(!dictionary.TryGetValue(data => data.Name, out var name) || string.IsNullOrWhiteSpace(name))
			return;

		//获取应用程序的标识
		var applicationId = this.DataAccess.Select<uint?>(
			Model.Naming.Get<Models.Application>(),
			Condition.Equal(nameof(Models.Application.Name), name),
			nameof(Models.Application.ApplicationId)).FirstOrDefault();

		if(applicationId.HasValue)
		{
			if(dictionary.TryGetValue(data => data.Edition, out var value) && !string.IsNullOrWhiteSpace(value) && value != "_")
			{
				var editionModel = Model.Build<ApplicationEdition>();
				editionModel.ApplicationId = applicationId.Value;
				editionModel.Name = value;
				editionModel.Title = value;
				this.DataAccess.Insert(editionModel, DataInsertOptions.IgnoreConstraint());
			}

			return;
		}

		var applicationModel = Model.Build<Models.Application>();
		applicationModel.Name = name;

		if(dictionary.TryGetValue(data => data.Title, out var title))
			applicationModel.Title = title;

		if(dictionary.TryGetValue(data => data.Edition, out var edition) && !string.IsNullOrWhiteSpace(edition) && edition != "_")
			applicationModel.Editions = [Model.Build<ApplicationEdition>(e => e.Name = e.Title = edition)];

		this.DataAccess.Insert(applicationModel, $"*,{nameof(Models.Application.Editions)}{{*}}", DataInsertOptions.IgnoreConstraint());
	}
	#endregion

	#region 嵌套子类
	/// <summary>表示发布属性的数据子服务类。</summary>
	public class PropertyService(ReleaseService service, DataServiceMutability? mutability = null) : DataServiceBase<ReleaseProperty>(service, mutability)
	{
		#region 重写方法
		protected override ICondition OnCondition(DataServiceMethod method, object[] values, IDataOptions options, out bool singular)
		{
			switch(values.Length)
			{
				case 1:
					singular = false;
					return Condition.Equal(nameof(ReleaseProperty.ReleaseId), values[0]);
				case 2:
					singular = true;
					return Condition.Equal(nameof(ReleaseProperty.ReleaseId), values[0]) &
					       Condition.Equal(nameof(ReleaseProperty.Name), values[1]);
			}

			return base.OnCondition(method, values, options, out singular);
		}

		protected override void OnModel(object[] values, IDataDictionary<ReleaseProperty> dictionary, IDataMutateOptions options)
		{
			if(values.Length != 1)
				throw new DataArgumentException(nameof(values));

			if(!Zongsoft.Common.Convert.TryConvertValue<uint>(values[0], out var releaseId) || releaseId == 0)
				throw new DataArgumentException(nameof(values));

			if(!dictionary.TryGetValue(p => p.Name, out var name) || string.IsNullOrWhiteSpace(name))
				throw new DataArgumentException(nameof(ReleaseProperty.Name));

			dictionary.SetValue(p => p.ReleaseId, releaseId);
		}
		#endregion
	}

	/// <summary>表示发布执行器的数据子服务类。</summary>
	public class ExecutorService(ReleaseService service, DataServiceMutability? mutability = null) : DataServiceBase<ReleaseExecutor>(service, mutability)
	{
		#region 重写方法
		protected override ICondition OnCondition(DataServiceMethod method, object[] values, IDataOptions options, out bool singular)
		{
			switch(values.Length)
			{
				case 1:
					singular = false;
					return Condition.Equal(nameof(ReleaseExecutor.ReleaseId), values[0]);
				case 2:
					singular = true;
					return Condition.Equal(nameof(ReleaseExecutor.ReleaseId), values[0]) &
					       Condition.Equal(nameof(ReleaseExecutor.SerialId), values[1]);
			}

			return base.OnCondition(method, values, options, out singular);
		}

		protected override void OnModel(object[] values, IDataDictionary<ReleaseExecutor> dictionary, IDataMutateOptions options)
		{
			if(values.Length != 1)
				throw new DataArgumentException(nameof(values));

			if(!Zongsoft.Common.Convert.TryConvertValue<uint>(values[0], out var releaseId) || releaseId == 0)
				throw new DataArgumentException(nameof(values));

			dictionary.SetValue(p => p.ReleaseId, releaseId);
		}
		#endregion
	}

	/// <summary>表示发布实例跟踪的数据子服务类。</summary>
	[DataService<ReleaseTracingCriteria>]
	public class TracingService(ReleaseService service, DataServiceMutability? mutability = null) : DataServiceBase<ReleaseTracing>(service, mutability)
	{
		#region 重写方法
		protected override ICondition OnCondition(DataServiceMethod method, object[] values, IDataOptions options, out bool singular)
		{
			switch(values.Length)
			{
				case 1:
					singular = false;
					return Condition.Equal(nameof(ReleaseTracing.ReleaseId), values[0]);
				case 2:
					singular = true;
					return Condition.Equal(nameof(ReleaseTracing.ReleaseId), values[0]) &
					       Condition.Equal(nameof(ReleaseTracing.InstanceId), values[1]);
			}

			return base.OnCondition(method, values, options, out singular);
		}

		protected override void OnModel(object[] values, IDataDictionary<ReleaseTracing> dictionary, IDataMutateOptions options)
		{
			if(values.Length != 1)
				throw new DataArgumentException(nameof(values));

			if(!Zongsoft.Common.Convert.TryConvertValue<uint>(values[0], out var releaseId) || releaseId == 0)
				throw new DataArgumentException(nameof(values));

			dictionary.SetValue(p => p.ReleaseId, releaseId);
		}
		#endregion
	}
	#endregion
}
