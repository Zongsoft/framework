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

using Zongsoft.Services;
using Zongsoft.Resources;

namespace Zongsoft.Data.Metadata;

public static class DataEntityUtility
{
	#region 静态方法
	public static IDataEntity Key(this IDataEntity entity, params string[] keys)
	{
		if(entity == null)
			return entity;
		if(keys == null || keys.Length == 0)
			return entity;

		for(int i = 0; i < keys.Length; i++)
		{
			if(string.IsNullOrEmpty(keys[i]))
				continue;

			if(entity.Properties.TryGetValue(keys[i], out var property) && property.IsSimplex(out var simplex))
				simplex.IsPrimaryKey = true;
		}

		return entity;
	}

	public static IDataEntity Property(this IDataEntity entity, string name, DataType type, bool nullable = true, bool immutable = false) => Property(entity, name, type, 0, nullable, immutable);
	public static IDataEntity Property(this IDataEntity entity, string name, DataType type, int length, bool nullable = true, bool immutable = false)
	{
		ArgumentNullException.ThrowIfNull(entity);
		entity.Properties.Simplex(name, type, length, nullable, immutable);
		return entity;
	}
	public static IDataEntity Property(this IDataEntity entity, string name, DataType type, byte precision, byte scale, bool nullable, bool immutable = false)
	{
		ArgumentNullException.ThrowIfNull(entity);
		entity.Properties.Simplex(name, type, precision, scale, nullable, immutable);
		return entity;
	}

	public static IDataEntity Property(this IDataEntity entity, string name, string port, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) => Property(entity, name, port, false, DataEntityComplexPropertyBehaviors.None, multiplicity, links);
	public static IDataEntity Property(this IDataEntity entity, string name, string port, bool immutable, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) => Property(entity, name, port, immutable, DataEntityComplexPropertyBehaviors.None, multiplicity, links);
	public static IDataEntity Property(this IDataEntity entity, string name, string port, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) => Property(entity, name, port, false, behaviors, multiplicity, links);
	public static IDataEntity Property(this IDataEntity entity, string name, string port, bool immutable, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links)
	{
		ArgumentNullException.ThrowIfNull(entity);
		entity.Properties.Complex(name, port, immutable, behaviors, multiplicity, links);
		return entity;
	}

	public static string GetTitle(this IDataEntity entity)
	{
		if(entity == null || ApplicationContext.Current == null)
			return null;

		IApplicationModule module;

		if(string.IsNullOrEmpty(entity.Namespace))
		{
			if(ApplicationContext.Current.Modules.TryGetValue(string.Empty, out module))
				return ResourceUtility.GetResourceString(module.Assembly, [$"{entity.Name}.Title", entity.Name]);

			if(ApplicationContext.Current.Modules.TryGetValue("Common", out module))
				return ResourceUtility.GetResourceString(module.Assembly, [$"{entity.Name}.Title", entity.Name]);
		}

		if(ApplicationContext.Current.Modules.TryGetValue(entity.Namespace, out module))
			return ResourceUtility.GetResourceString(module.Assembly, [$"{entity.Name}.Title", entity.Name]);

		return null;
	}

	public static string GetDescription(this IDataEntity entity)
	{
		if(entity == null || ApplicationContext.Current == null)
			return null;

		IApplicationModule module;

		if(string.IsNullOrEmpty(entity.Namespace))
		{
			if(ApplicationContext.Current.Modules.TryGetValue(string.Empty, out module))
				return ResourceUtility.GetResourceString(module.Assembly, $"{entity.Name}.Description");

			if(ApplicationContext.Current.Modules.TryGetValue("Common", out module))
				return ResourceUtility.GetResourceString(module.Assembly, $"{entity.Name}.Description");
		}

		if(ApplicationContext.Current.Modules.TryGetValue(entity.Namespace, out module))
			return ResourceUtility.GetResourceString(module.Assembly, $"{entity.Name}.Description");

		return null;
	}

	/// <summary>获取指定的数据实体，如果指定的定位标识为完整的限定名(即包含命名空间)则返回该限定名的数据实体；否则将指定实体的命名空间作为查找的命名空间。</summary>
	/// <param name="entity">定位参考的数据实体。</param>
	/// <param name="locator">要查找的定位标识，如果不是完整的限定名则采用 <paramref name="entity"/> 参数的实体命名空间作为定位空间。</param>
	/// <returns>返回定位成功的数据实体。</returns>
	/// <exception cref="System.Collections.Generic.KeyNotFoundException">当定位失败则抛出该异常。</exception>
	public static IDataEntity Locate(IDataEntity entity, string locator)
	{
		if(string.IsNullOrEmpty(locator))
			return null;

		(var name, var @namespace) = DataUtility.Qualify(locator);

		if(string.IsNullOrEmpty(@namespace) && !string.IsNullOrEmpty(entity?.Namespace) &&
		   Mapping.Entities.TryGetValue(name, entity.Namespace, out var found))
		{
			return found;
		}

		return Mapping.Entities.TryGetValue(name, @namespace, out var result) ?
			result : throw new System.Collections.Generic.KeyNotFoundException(string.Format(Properties.Resources.DataEntity_NotFound_Message, locator));
	}
	#endregion
}
