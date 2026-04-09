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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Reflection;
using Zongsoft.Collections;

namespace Zongsoft.Data.Common;

public abstract class DataImporterBase : IDataImporter
{
	#region 导入方法
	public void Import(DataImportContext context) => this.OnImport(context, GetMembers(context));
	public ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default) => this.OnImportAsync(context, GetMembers(context), cancellation);
	#endregion

	#region 抽象方法
	protected abstract void OnImport(DataImportContext context, MemberCollection members);
	protected abstract ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default);
	#endregion

	#region 私有方法
	private static MemberCollection GetMembers(DataImportContext context)
	{
		var members = new MemberCollection();

		if(typeof(System.Collections.IDictionary).IsAssignableFrom(context.ModelType))
			GenerateMembersWithoutInfo(context, members);
		else
			GenerateMembersWithInfo(context, members);

		return members;

		static MemberInfo GetMemberInfo(Type type, string name) =>
			(MemberInfo)type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) ??
			(MemberInfo)type.GetField(name, BindingFlags.Public | BindingFlags.Instance);

		static void GenerateMembersWithInfo(DataImportContext context, MemberCollection members)
		{
			if(context.Members == null || context.Members.Length == 0)
			{
				foreach(var property in context.Entity.Properties)
				{
					if(property.IsComplex)
						continue;

					var info = GetMemberInfo(context.ModelType, property.Name);
					if(info != null)
						members.Add(new Member(context, (Metadata.IDataEntitySimplexProperty)property, info));
				}
			}
			else
			{
				for(int i = 0; i < context.Members.Length; i++)
				{
					if(context.Entity.Properties.TryGetValue(context.Members[i], out var property))
					{
						if(property.IsComplex)
							throw new DataException($"The specified '{property.Name}' property cannot be a navigation property, only scalar field data can be import.");

						var info = GetMemberInfo(context.ModelType, property.Name);
						if(info != null)
							members.Add(new Member(context, (Metadata.IDataEntitySimplexProperty)property, info));
					}
				}
			}
		}

		static void GenerateMembersWithoutInfo(DataImportContext context, MemberCollection members)
		{
			if(context.Members == null || context.Members.Length == 0)
			{
				foreach(var property in context.Entity.Properties)
				{
					if(property.IsComplex)
						continue;

					members.Add(new Member(context, (Metadata.IDataEntitySimplexProperty)property, null));
				}
			}
			else
			{
				for(int i = 0; i < context.Members.Length; i++)
				{
					if(context.Entity.Properties.TryGetValue(context.Members[i], out var property))
					{
						if(property.IsComplex)
							throw new DataException($"The specified '{property.Name}' property cannot be a navigation property, only scalar field data can be import.");

						members.Add(new Member(context, (Metadata.IDataEntitySimplexProperty)property, null));
					}
				}
			}
		}
	}
	#endregion

	#region 嵌套结构
	public readonly struct Member
	{
		#region 私有变量
		private readonly DataImportContextBase _context;
		private readonly IDataValidator _validator;
		private readonly Delegate _getter;
		private readonly Delegate _setter;
		private readonly bool _isIndexer;
		#endregion

		#region 构造函数
		public Member(DataImportContextBase context, Metadata.IDataEntityProperty property, MemberInfo member)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_validator = DataEnvironment.Validators.GetValidator(context);
			this.Property = property ?? throw new ArgumentNullException(nameof(property));
			this.Name = property.Name;

			switch(member)
			{
				case FieldInfo fieldInfo:
					this.Type = fieldInfo.FieldType;
					_getter = fieldInfo.GetGetter();
					_setter = fieldInfo.GetSetter();
					break;
				case PropertyInfo propertyInfo:
					this.Type = propertyInfo.PropertyType;
					_getter = propertyInfo.GetGetter();
					_setter = propertyInfo.GetSetter();
					_isIndexer = propertyInfo.GetIndexParameters().Length > 0;
					break;
				default:
					if(member != null)
						throw new ArgumentException($"The specified '{member.Name}' info is invalid member.");
					break;
			}
		}
		#endregion

		#region 公共字段
		public readonly string Name;
		public readonly Type Type;
		public readonly Metadata.IDataEntityProperty Property;
		#endregion

		#region 公共方法
		public bool IsSimplex(out Metadata.IDataEntitySimplexProperty simplex)
		{
			simplex = this.Property as Metadata.IDataEntitySimplexProperty;
			return simplex != null;
		}

		public bool IsComplex(out Metadata.IDataEntityComplexProperty complex)
		{
			complex = this.Property as Metadata.IDataEntityComplexProperty;
			return complex != null;
		}

		public object GetValue(ref object target)
		{
			object value;

			if(!this.IsSimplex(out var property))
				return null;

			//判读当前属性是否为Sequence字段
			if(CanSequence(property.Sequence))
			{
				//获取目标的当前属性值，如果获取失败或其值为空或数字零，则递增该字段序号
				if(!this.TryGetMemberValue(ref target, out value) || value == null || Convert.IsDBNull(value) || Zongsoft.Common.Convert.IsZero(value))
				{
					//递增当前属性对应的序号
					var id = _context.DataAccess.Sequencer.Increase(property);

					//尝试将递增的序号值写入到目标对象的属性
					this.SetMemberValue(ref target, Zongsoft.Common.Convert.ConvertValue(id, this.Type));

					//返回最新的序号值
					return Convert.ChangeType(id, property.Type.DbType.AsType());
				}
			}

			//验证当前属性是否需要强制更新其值
			if(_validator != null && _validator.OnImport(_context, this.Property, out value))
			{
				//尝试验证器返回的值写入到目标对象的属性
				this.SetMemberValue(ref target, value);

				//返回验证后的值
				return ConvertValue(value, property.Type, property.Length, property.Nullable);
			}

			if(this.TryGetMemberValue(ref target, out value))
				return ConvertValue(value == null || value is string text && string.IsNullOrEmpty(text) ? property.DefaultValue : value, property.Type, property.Length, property.Nullable);
			else
				return ConvertValue(property.DefaultValue, property.Type, property.Length, property.Nullable);

			static object ConvertValue(object value, DbType type, int length, bool nullable)
			{
				//处理枚举类型的值，将枚举类型转换为其基元类型
				if(value is not null && value.GetType().IsEnum)
					return Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));

				//如果待转换的值为字符串且字段类型为字符串类型且指定了长度限制，则进行截取处理
				if(value is string text && length > 0 && IsString(type))
					return text.Length > length ? text[..length] : text;

				//如果待转换的值不为空且当前字段不允许空，则尝试获取其类型的默认值
				return value == null && !nullable ? Zongsoft.Common.TypeExtension.GetDefaultValue(DataUtility.AsType(type)) : value;
			}

			static bool IsString(DbType dbType) =>
				dbType == DbType.AnsiString ||
				dbType == DbType.AnsiStringFixedLength ||
				dbType == DbType.String ||
				dbType == DbType.StringFixedLength ||
				dbType == DbType.Xml;
		}
		#endregion

		#region 重写方法
		public override string ToString() => this.Property.ToString();
		#endregion

		#region 私有方法
		private bool TryGetMemberValue(ref object target, out object value)
		{
			if(target is System.Collections.IDictionary dictionary)
				return dictionary.TryGetValue(this.Name, out value);

			if(_getter is FieldInfoExtension.Getter getter)
				value = getter.Invoke(ref target);
			else
				value = _isIndexer ?
					((PropertyInfoExtension.Getter)_getter).Invoke(ref target, this.Name) :
					((PropertyInfoExtension.Getter)_getter).Invoke(ref target);

			return true;
		}

		private void SetMemberValue(ref object target, object value)
		{
			if(target is System.Collections.IDictionary dictionary)
			{
				dictionary[this.Name] = value;
				return;
			}

			if(_setter is FieldInfoExtension.Setter setter)
				setter.Invoke(ref target, value);
			else if(_isIndexer)
				((PropertyInfoExtension.Setter)_setter).Invoke(ref target, value, this.Name);
			else
				((PropertyInfoExtension.Setter)_setter).Invoke(ref target, value);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool CanSequence(Metadata.IDataEntityPropertySequence sequence) =>
			sequence != null &&
			sequence.IsExternal &&
			(sequence.References == null || sequence.References.Length == 0);
		#endregion
	}

	public sealed class MemberCollection : System.Collections.ObjectModel.KeyedCollection<string, Member>
	{
		protected override string GetKeyForItem(Member member) => member.Name;
	}
	#endregion
}
