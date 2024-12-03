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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Data.Common
{
	public abstract class DataImporterBase : IDataImporter
	{
		#region 构造函数
		protected DataImporterBase(DataImportContextBase context)
		{
			static MemberInfo GetMemberInfo(Type type, string name) =>
				(MemberInfo)type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) ??
				(MemberInfo)type.GetField(name, BindingFlags.Public | BindingFlags.Instance);

			if(context.Members == null || context.Members.Length == 0)
			{
				var members = new List<Member>(context.Entity.Properties.Count);

				foreach(var property in context.Entity.Properties)
				{
					if(property.IsComplex)
						continue;

					var info = GetMemberInfo(context.ModelType, property.Name);
					if(info != null)
						members.Add(new Member(context, info, (Metadata.IDataEntitySimplexProperty)property));
				}

				this.Members = members.ToArray();
			}
			else
			{
				var members = new List<Member>(context.Members.Length);

				for(int i = 0; i < context.Members.Length; i++)
				{
					if(context.Entity.Properties.TryGetValue(context.Members[i], out var property))
					{
						if(property.IsComplex)
							throw new DataException($"The specified '{property.Name}' property cannot be a navigation property, only scalar field data can be import.");

						var info = GetMemberInfo(context.ModelType, property.Name);
						if(info != null)
							members.Add(new Member(context, info, (Metadata.IDataEntitySimplexProperty)property));
					}
				}

				this.Members = members.ToArray();
			}
		}
		#endregion

		#region 公共属性
		public Member[] Members { get; }
		#endregion

		#region 导入方法
		public abstract void Import(DataImportContext context);
		public abstract ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default);
		#endregion

		#region 处置方法
		public void Dispose()
		{
			var disposed = Interlocked.CompareExchange(ref _disposed, 1, 0);

			if(disposed == 0)
			{
				this.Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		private volatile int _disposed;
		protected virtual void Dispose(bool disposing) { }
		#endregion

		#region 嵌套结构
		public readonly struct Member(DataImportContextBase context, MemberInfo info, Metadata.IDataEntitySimplexProperty property)
		{
			#region 私有变量
			private readonly DataImportContextBase _context = context ?? throw new ArgumentNullException(nameof(context));
			private readonly IDataValidator _validator = DataEnvironment.Validators.GetValidator(context);
			#endregion

			#region 公共属性
			public string Name => this.Info.Name;
			public readonly MemberInfo Info = info ?? throw new ArgumentNullException(nameof(info));
			public readonly Metadata.IDataEntitySimplexProperty Property = property ?? throw new ArgumentNullException(nameof(property));
			#endregion

			#region 公共方法
			public object GetValue(ref object target)
			{
				object value;

				//判读当前属性是否为Sequence字段
				if(CanSequence(this.Property.Sequence))
				{
					//获取目标的当前属性值，如果获取失败或其值为空或数字零，则递增该字段序号
					if(!Reflection.Reflector.TryGetValue(this.Info, ref target, out value) || value == null || Zongsoft.Common.Convert.IsZero(value))
					{
						//递增当前属性对应的序号
						var id = _context.DataAccess.Sequencer.Increase(this.Property);

						//尝试将递增的序号值写入到目标对象的属性
						Reflection.Reflector.TrySetValue(this.Info, ref target, type => Zongsoft.Common.Convert.ConvertValue(id, type));

						//返回最新的序号值
						return id;
					}
				}

				//验证当前属性是否需要强制更新其值
				if(_validator != null && _validator.OnImport(_context, this.Property, out value))
				{
					//尝试验证器返回的值写入到目标对象的属性
					Reflection.Reflector.TrySetValue(this.Info, ref target, value);

					//返回验证后的值
					return GetUnderlyingType(value, this.Property.Type, this.Property.Nullable);
				}

				if(Reflection.Reflector.TryGetValue(this.Info, ref target, out value))
					return GetUnderlyingType(value == null || value is string text && string.IsNullOrEmpty(text) ? this.Property.DefaultValue : value, this.Property.Type, this.Property.Nullable);
				else
					return GetUnderlyingType(this.Property.DefaultValue, this.Property.Type, this.Property.Nullable);

				//处理枚举类型的值，将枚举类型转换为其基元类型
				static object GetUnderlyingType(object value, DbType type, bool nullable)
				{
					if(value is not null && value.GetType().IsEnum)
						return Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));

					//如果待转换的值不为空且当前字段不允许空，则尝试获取其类型的默认值
					return value == null && !nullable ? Zongsoft.Common.TypeExtension.GetDefaultValue(DbTypeUtility.AsType(type)) : value;
				}
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private static bool CanSequence(Metadata.IDataEntityPropertySequence sequence) =>
				sequence != null &&
				sequence.IsExternal &&
				(sequence.References == null || sequence.References.Length == 0);
			#endregion
		}
		#endregion
	}
}
