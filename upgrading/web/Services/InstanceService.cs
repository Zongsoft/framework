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

using Zongsoft.Data;
using Zongsoft.Upgrading.Models;

namespace Zongsoft.Upgrading.Services;

/// <summary>表示实例的数据服务基类。</summary>
[DataService<InstanceCriteria>]
public class InstanceService(IServiceProvider serviceProvider, DataServiceMutability? mutability = null) : DataServiceBase<Instance>(serviceProvider, mutability ?? DataServiceMutability.All)
{
	#region 公共属性
	/// <summary>获取发布跟踪子服务。</summary>
	public TracingService Tracings => this.GetSubservice<TracingService>();
	#endregion

	#region 嵌套子类
	/// <summary>表示实例发布跟踪的数据子服务类。</summary>
	[DataService<ReleaseTracingCriteria>]
	public class TracingService(InstanceService service, DataServiceMutability? mutability = null) : DataServiceBase<ReleaseTracing>(service, mutability)
	{
		#region 重写方法
		protected override ICondition OnCondition(DataServiceMethod method, object[] values, IDataOptions options, out bool singular)
		{
			switch(values.Length)
			{
				case 1:
					singular = false;
					return Condition.Equal(nameof(ReleaseTracing.InstanceId), values[0]);
				case 2:
					singular = true;
					return Condition.Equal(nameof(ReleaseTracing.InstanceId), values[0]) &
					       Condition.Equal(nameof(ReleaseTracing.ReleaseId), values[1]);
			}

			return base.OnCondition(method, values, options, out singular);
		}

		protected override void OnModel(object[] values, IDataDictionary<ReleaseTracing> dictionary, IDataMutateOptions options)
		{
			if(values.Length != 1)
				throw new DataArgumentException(nameof(values));

			if(!Zongsoft.Common.Convert.TryConvertValue<uint>(values[0], out var instanceId) || instanceId == 0)
				throw new DataArgumentException(nameof(values));

			dictionary.SetValue(p => p.InstanceId, instanceId);
		}
		#endregion
	}
	#endregion
}
