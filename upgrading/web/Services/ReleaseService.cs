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
	/// <summary>获取发布状态子服务。</summary>
	public PublishingService Publishings => this.GetSubservice<PublishingService>();
	#endregion

	#region 公共方法
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
			var count = await this.UpdateAsync(new
			{
				Path = value,
				Size = (ulong)size,
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
	}
	#endregion

	#region 重写方法
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

	/// <summary>表示发布实例状态的数据子服务类。</summary>
	[DataService<ReleasePublishingCriteria>]
	public class PublishingService(ReleaseService service, DataServiceMutability? mutability = null) : DataServiceBase<ReleasePublishing>(service, mutability)
	{
		#region 重写方法
		protected override ICondition OnCondition(DataServiceMethod method, object[] values, IDataOptions options, out bool singular)
		{
			switch(values.Length)
			{
				case 1:
					singular = false;
					return Condition.Equal(nameof(ReleasePublishing.ReleaseId), values[0]);
				case 2:
					singular = true;
					return Condition.Equal(nameof(ReleasePublishing.ReleaseId), values[0]) &
					       Condition.Equal(nameof(ReleasePublishing.InstanceId), values[1]);
			}

			return base.OnCondition(method, values, options, out singular);
		}

		protected override void OnModel(object[] values, IDataDictionary<ReleasePublishing> dictionary, IDataMutateOptions options)
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
