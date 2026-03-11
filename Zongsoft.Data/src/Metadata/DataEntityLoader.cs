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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata;

[Services.Service<Mapping.Loader>(Members = nameof(Instance))]
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
		var entities = new List<IDataEntity>();
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();

		for(int i = 0; i < assemblies.Length; i++)
		{
			var assembly = assemblies[i];

			if(assembly.IsDynamic)
				continue;

			foreach(var type in GetTypes(assembly))
			{
				var entity = GetEntity(type);

				if(entity != null)
					entities.Add(entity);
			}
		}

		return [new Result(entities, [])];
	}
	#endregion

	#region 私有方法
	private static Type[] GetTypes(Assembly assembly)
	{
		try {
			return assembly.GetExportedTypes();
		}
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
