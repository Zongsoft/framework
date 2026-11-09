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
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata;

//[Services.Service<Mapping.Loader>(Members = nameof(Instance))]
public class DataEntityLoader : Mapping.Loader
{
	#region 单例字段
	public static readonly DataEntityLoader Instance = new();
	#endregion

	#region 私有构造
	private DataEntityLoader() { }
	#endregion

	#region 重写方法
	protected override IEnumerable<Result> OnLoad()
	{
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();

		for(int i = 0; i < assemblies.Length; i++)
		{
			var assembly = assemblies[i];

			if(assembly.IsDynamic)
				continue;

			var entities = new List<IDataEntity>();

			foreach(var type in GetTypes(assembly))
			{
				var entity = GetEntity(type);

				if(entity != null)
					entities.Add(entity);
			}

			yield return new(assembly.GetName().Name, entities, []);
		}
	}
	#endregion

	#region 私有方法
	private static Type[] GetTypes(Assembly assembly)
	{
		try { return assembly.GetExportedTypes(); }
		catch { return []; }
	}

	private static IDataEntity GetEntity(Type type)
	{
		if(type == null || type.IsNotPublic || type.IsGenericType || type.IsEnum || type.IsArray || type.IsPrimitive)
			return null;

		if(type.IsDefined(typeof(ModelAttribute)))
		{
			var entity = Model.GetDescriptor(type).ToEntity();

			if(entity != null && entity.Properties.Count > 0)
				return entity;
		}

		return null;
	}
	#endregion
}
