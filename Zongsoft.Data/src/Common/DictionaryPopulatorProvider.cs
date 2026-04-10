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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Data;

namespace Zongsoft.Data.Common;

public class DictionaryPopulatorProvider : IDataPopulatorProvider
{
	#region 单例模式
	public static readonly DictionaryPopulatorProvider Instance = new();
	#endregion

	#region 构造函数
	private DictionaryPopulatorProvider() { }
	#endregion

	#region 公共方法
	public bool CanPopulate(Type type) => Zongsoft.Common.TypeExtension.IsDictionary(type);
	public IDataPopulator GetPopulator(IDataDriver driver, Type type, IDataRecord record, Metadata.IDataEntity entity = null)
	{
		var properties = new Metadata.IDataEntityProperty[record.FieldCount];

		for(int i = 0; i < record.FieldCount; i++)
		{
			//获取字段名对应的属性名（注意：由查询引擎确保返回的记录列名就是属性名）
			properties[i] = GetProperty(entity, record.GetName(i));
		}

		return new DictionaryPopulator(type, properties);
	}
	#endregion

	#region 私有方法
	private Metadata.IDataEntityProperty GetProperty(Metadata.IDataEntity entity, string path)
	{
		if(entity.Properties.TryGetValue(path, out var property))
			return property;

		var parts = path.Split('.');

		if(parts.Length > 1)
		{
			for(int i = 0; i < parts.Length; i++)
			{
				if(entity == null || !entity.Properties.TryGetValue(parts[i], out property))
					return null;

				if(property is Metadata.IDataEntityComplexProperty complex)
					entity = complex.Foreign;
				else
					entity = null;
			}
		}

		//确保属性地址指向的最终属性不能是导航属性
		return property == null || property.IsComplex ? null : property;
	}
	#endregion
}
