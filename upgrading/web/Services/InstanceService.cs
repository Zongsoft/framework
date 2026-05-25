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

using Zongsoft.Data;
using Zongsoft.Upgrading.Models;

namespace Zongsoft.Upgrading.Services;

/// <summary>表示实例的数据服务基类。</summary>
[DataService<InstanceCriteria>]
public class InstanceService(IServiceProvider serviceProvider, DataServiceMutability? mutability = null) : DataServiceBase<Instance>(serviceProvider, mutability ?? DataServiceMutability.All)
{
	#region 公共属性
	/// <summary>获取发布状态子服务。</summary>
	public PublishingService Publishings => this.GetSubservice<PublishingService>();
	#endregion

	#region 嵌套子类
	/// <summary>表示实例发布状态的数据子服务类。</summary>
	[DataService<ReleasePublishingCriteria>]
	public class PublishingService(InstanceService service, DataServiceMutability? mutability = null) : DataServiceBase<ReleasePublishing>(service, mutability)
	{
		#region 重写方法
		protected override ICondition OnCondition(DataServiceMethod method, object[] values, IDataOptions options, out bool singular)
		{
			switch(values.Length)
			{
				case 1:
					singular = false;
					return Condition.Equal(nameof(ReleasePublishing.InstanceId), values[0]);
				case 2:
					singular = true;
					return Condition.Equal(nameof(ReleasePublishing.InstanceId), values[0]) &
					       Condition.Equal(nameof(ReleasePublishing.ReleaseId), values[1]);
			}

			return base.OnCondition(method, values, options, out singular);
		}

		protected override void OnModel(object[] values, IDataDictionary<ReleasePublishing> dictionary, IDataMutateOptions options)
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
