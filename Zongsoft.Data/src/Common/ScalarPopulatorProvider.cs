/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.ComponentModel;
using System.Collections.Concurrent;

namespace Zongsoft.Data.Common
{
	public class ScalarPopulatorProvider : IDataPopulatorProvider
	{
		#region 单例模式
		public static readonly ScalarPopulatorProvider Instance = new ScalarPopulatorProvider();
		#endregion

		#region 私有变量
		private readonly ConcurrentDictionary<Type, ConverterPopulater> _converters;
		#endregion

		#region 构造函数
		private ScalarPopulatorProvider()
		{
			_converters = new ConcurrentDictionary<Type, ConverterPopulater>();
		}
		#endregion

		#region 公共方法
		public bool CanPopulate(Type type)
		{
			return Zongsoft.Common.TypeExtension.IsScalarType(type);
		}

		public IDataPopulator GetPopulator(Metadata.IDataEntity entity, Type type, IDataReader reader)
		{
			//获取指定类型对应的装配器
			var populator = this.GetPopulator(type);

			if(populator == null)
			{
				//如果是可空类型，则获取可空类型的定义元类型
				if(type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					populator = this.GetPopulator(type.GetGenericArguments()[0]);
				}
			}

			return populator;
		}
		#endregion

		#region 私有方法
		private IDataPopulator GetPopulator(Type type)
		{
			switch(Type.GetTypeCode(type))
			{
				case TypeCode.Char:
					return ScalarPopulator.Char;
				case TypeCode.String:
					return ScalarPopulator.String;
				case TypeCode.Boolean:
					return ScalarPopulator.Boolean;
				case TypeCode.DateTime:
					return ScalarPopulator.DateTime;
				case TypeCode.Byte:
					return ScalarPopulator.Byte;
				case TypeCode.SByte:
					return ScalarPopulator.SByte;
				case TypeCode.Int16:
					return ScalarPopulator.Int16;
				case TypeCode.Int32:
					return ScalarPopulator.Int32;
				case TypeCode.Int64:
					return ScalarPopulator.Int64;
				case TypeCode.UInt16:
					return ScalarPopulator.UInt16;
				case TypeCode.UInt32:
					return ScalarPopulator.UInt32;
				case TypeCode.UInt64:
					return ScalarPopulator.UInt64;
				case TypeCode.Single:
					return ScalarPopulator.Single;
				case TypeCode.Double:
					return ScalarPopulator.Double;
				case TypeCode.Decimal:
					return ScalarPopulator.Decimal;
			}

			if(type == typeof(Guid))
				return ScalarPopulator.Guid;

			if(type == typeof(DateTimeOffset))
				return ScalarPopulator.DateTimeOffset;

			return _converters.GetOrAdd(type, t => new ConverterPopulater(t));
		}
		#endregion

		#region 嵌套子类
		private class ConverterPopulater : IDataPopulator
		{
			#region 私有变量
			private TypeConverter _converter;
			#endregion

			#region 构造函数
			public ConverterPopulater(Type type)
			{
				_converter = TypeDescriptor.GetConverter(type) ?? throw new InvalidOperationException($"The specified '{type.FullName}' type has no type converter.");

				if(!_converter.CanConvertFrom(typeof(string)))
					throw new InvalidOperationException($"The '{_converter.GetType().Name}' type converter does not support converting from a string to a '{type.FullName}' target type.");
			}
			#endregion

			#region 公共方法
			public object Populate(IDataRecord record)
			{
				return _converter.ConvertFromString(record.GetString(0));
			}
			#endregion
		}
		#endregion
	}
}
