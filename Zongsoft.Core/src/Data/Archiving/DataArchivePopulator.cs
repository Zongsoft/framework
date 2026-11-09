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

using Zongsoft.Reflection;

namespace Zongsoft.Data.Archiving;

public class DataArchivePopulator : IDataArchivePopulator
{
	#region 单例字段
	public static readonly DataArchivePopulator Default = new();
	#endregion

	#region 公共方法
	public T Populate<T>(IDataArchiveRecord record, ModelDescriptor descriptor)
	{
		//构建数据实例
		var target = this.Build<T>();

		for(int i = 0; i < record.FieldCount; i++)
		{
			var field = record.GetName(i);
			if(string.IsNullOrEmpty(field))
				continue;

			if(descriptor.Properties.TryGetValue(field, out var property))
				Reflector.TrySetValue(property.Member, ref target, type => Common.Convert.ConvertValue(record.GetValue(i), type));
			else
				this.Unrecognize(ref target, field, record.GetValue(i));
		}

		//执行组装完成
		this.OnPopulated(ref target, record, descriptor);

		return target;
	}
	#endregion

	#region 虚拟方法
	protected virtual T Build<T>() => typeof(T).IsInterface || typeof(T).IsAbstract ? Model.Build<T>() : Activator.CreateInstance<T>();
	protected virtual void Unrecognize<T>(ref T target, string field, object value) { }
	protected virtual void OnPopulated<T>(ref T target, IDataArchiveRecord record, ModelDescriptor descriptor) { }
	#endregion
}
