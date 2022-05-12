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
		private readonly ConcurrentDictionary<Type, IDataPopulator> _converters;
		#endregion

		#region 构造函数
		private ScalarPopulatorProvider()
		{
			_converters = new ConcurrentDictionary<Type, IDataPopulator>();
		}
		#endregion

		#region 公共方法
		public bool CanPopulate(Type type) => Zongsoft.Common.TypeExtension.IsScalarType(type);
		public IDataPopulator<T> GetPopulator<T>(IDataRecord record, Metadata.IDataEntity entity = null) => this.GetPopulator(typeof(T), record, entity) as IDataPopulator<T>;
		public IDataPopulator GetPopulator(Type type, IDataRecord record, Metadata.IDataEntity entity = null) => Zongsoft.Common.TypeExtension.IsNullable(type, out var underlyingType) ?
			this.GetPopulator(underlyingType, true) :
			this.GetPopulator(type, false);
		#endregion

		#region 私有方法
		private IDataPopulator GetPopulator(Type type, bool nullable)
		{
			switch(Type.GetTypeCode(type))
			{
				case TypeCode.Char:
					return nullable ? NullablePopulator.Char : ScalarPopulator.Char;
				case TypeCode.String:
					return ScalarPopulator.String;
				case TypeCode.Boolean:
					return nullable ? NullablePopulator.Boolean : ScalarPopulator.Boolean;
				case TypeCode.DateTime:
					return nullable ? NullablePopulator.DateTime : ScalarPopulator.DateTime;
				case TypeCode.Byte:
					return nullable ? NullablePopulator.Byte : ScalarPopulator.Byte;
				case TypeCode.SByte:
					return nullable ? NullablePopulator.SByte : ScalarPopulator.SByte;
				case TypeCode.Int16:
					return nullable ? NullablePopulator.Int16 : ScalarPopulator.Int16;
				case TypeCode.Int32:
					return nullable ? NullablePopulator.Int32 : ScalarPopulator.Int32;
				case TypeCode.Int64:
					return nullable ? NullablePopulator.Int64 : ScalarPopulator.Int64;
				case TypeCode.UInt16:
					return nullable ? NullablePopulator.UInt16 : ScalarPopulator.UInt16;
				case TypeCode.UInt32:
					return nullable ? NullablePopulator.UInt32 : ScalarPopulator.UInt32;
				case TypeCode.UInt64:
					return nullable ? NullablePopulator.UInt64 : ScalarPopulator.UInt64;
				case TypeCode.Single:
					return nullable ? NullablePopulator.Single : ScalarPopulator.Single;
				case TypeCode.Double:
					return nullable ? NullablePopulator.Double : ScalarPopulator.Double;
				case TypeCode.Decimal:
					return nullable ? NullablePopulator.Decimal : ScalarPopulator.Decimal;
			}

			if(type == typeof(Guid))
				return nullable ? NullablePopulator.Guid : ScalarPopulator.Guid;

			if(type == typeof(DateTimeOffset))
				return nullable ? NullablePopulator.DateTimeOffset : ScalarPopulator.DateTimeOffset;

			if(type == typeof(byte[]))
				return nullable ? NullablePopulator.Bytes : ScalarPopulator.Bytes;

			if(type == typeof(char[]))
				return nullable ? NullablePopulator.Chars : ScalarPopulator.Chars;

			return _converters.GetOrAdd(type, type => (IDataPopulator)Activator.CreateInstance(typeof(ConverterPopulater<>).MakeGenericType(type)));
		}

		public IDataPopulator<T> GetPopulator<T>(bool nullable)
		{
			return this.GetPopulator(typeof(T), nullable) as IDataPopulator<T>;
		}
		#endregion

		#region 嵌套子类
		private class ConverterPopulater<T> : IDataPopulator, IDataPopulator<T>
		{
			#region 私有变量
			private readonly TypeConverter _converter;
			#endregion

			#region 构造函数
			public ConverterPopulater()
			{
				_converter = TypeDescriptor.GetConverter(typeof(T)) ?? throw new InvalidOperationException($"The specified '{typeof(T).FullName}' type has no type converter.");
			}
			#endregion

			#region 公共方法
			object IDataPopulator.Populate(IDataRecord record)
			{
				object value = record.IsDBNull(0) ? null : record.GetValue(0);

				if(_converter.CanConvertFrom(record.GetFieldType(0)))
					return _converter.ConvertFrom(value);

				if(_converter.CanConvertTo(typeof(T)))
					return _converter.ConvertTo(value, typeof(T));

				throw new InvalidOperationException($"The specified '{_converter.GetType().FullName}' type converter does not support conversion from the '{record.GetFieldType(0)?.FullName}' source type nor does it support conversion to the '{typeof(T).FullName}' target type.");
			}

			TResult IDataPopulator.Populate<TResult>(IDataRecord record)
			{
				object value = record.IsDBNull(0) ? null : record.GetValue(0);

				if(_converter.CanConvertFrom(record.GetFieldType(0)))
					return (TResult)_converter.ConvertFrom(value);

				if(_converter.CanConvertTo(typeof(TResult)))
					return (TResult)_converter.ConvertTo(value, typeof(TResult));

				throw new InvalidOperationException($"The specified '{_converter.GetType().FullName}' type converter does not support conversion from the '{record.GetFieldType(0)?.FullName}' source type nor does it support conversion to the '{typeof(TResult).FullName}' target type.");
			}

			public T Populate(IDataRecord record)
			{
				object value = record.IsDBNull(0) ? null : record.GetValue(0);

				if(_converter.CanConvertFrom(record.GetFieldType(0)))
					return (T)_converter.ConvertFrom(value);

				if(_converter.CanConvertTo(typeof(T)))
					return (T)_converter.ConvertTo(value, typeof(T));

				throw new InvalidOperationException($"The specified '{_converter.GetType().FullName}' type converter does not support conversion from the '{record.GetFieldType(0)?.FullName}' source type nor does it support conversion to the '{typeof(T).FullName}' target type.");
			}
			#endregion
		}
		#endregion
	}
}
