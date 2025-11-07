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
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Data.Common;

public class ModelPopulator<TModel> : IDataPopulator, IDataPopulator<TModel>
{
	#region 私有变量
	private static readonly Func<IDataRecord, TModel> Creator = typeof(TModel).IsAbstract ?
		record => Model.Build<TModel>() :
		record => System.Activator.CreateInstance<TModel>();
	#endregion

	#region 成员字段
	private readonly Metadata.IDataEntity _entity;
	private readonly MemberMappingCollection _members;
	#endregion

	#region 构造函数
	public ModelPopulator(Metadata.IDataEntity entity)
	{
		_entity = entity;
		_members = new MemberMappingCollection();
	}
	#endregion

	#region 公共属性
	public Metadata.IDataEntity Entity => _entity;
	internal MemberMappingCollection Members => _members;
	#endregion

	#region 公共方法
	public TModel Populate(IDataRecord record) => Populate(record, _members);
	TResult IDataPopulator.Populate<TResult>(IDataRecord record) => (TResult)(object)Populate(record, _members);
	#endregion

	#region 私有方法
	private static TModel Populate(IDataRecord record, ICollection<MemberMapping> members)
	{
		TModel model = default;

		if(members == null || members.Count == 0)
		{
			if(record.FieldCount > 0)
				model = record.GetValue(0) is TModel value ? value : default;

			return model;
		}

		foreach(var member in members)
		{
			if(member.Ordinal >= 0)
			{
				model ??= Creator.Invoke(record);
				member.Token.Populate(ref model, record, member.Ordinal);
			}
			else if(member.Populator != null)
			{
				model ??= Creator.Invoke(record);
				member.Token.SetValue(ref model, member.Populate(record));
			}
		}

		return model;
	}
	#endregion

	#region 嵌套子类
	internal readonly struct MemberMapping
	{
		#region 公共字段
		public readonly int Ordinal;
		public readonly ModelMemberToken<TModel> Token;
		public readonly IDataPopulator Populator;
		#endregion

		#region 构造函数
		public MemberMapping(ModelMemberToken<TModel> token, int ordinal)
		{
			this.Token = token;
			this.Ordinal = ordinal;
			this.Populator = null;
		}

		public MemberMapping(ModelMemberToken<TModel> token, IDataPopulator populator)
		{
			this.Token = token;
			this.Populator = populator;
			this.Ordinal = -1;
		}
		#endregion

		#region 内部方法
		internal object Populate(IDataRecord record)
		{
			if(this.Populator != null)
				return this.Populator.Populate<object>(record);

			return default;
		}
		#endregion

		#region 重写方法
		public override string ToString() => this.Ordinal < 0 ?
			$"{this.Token}({this.Populator})" :
			$"{this.Token}#{this.Ordinal}";
		#endregion
	}

	internal class MemberMappingCollection : System.Collections.ObjectModel.KeyedCollection<string, MemberMapping>
	{
		public MemberMappingCollection() : base(StringComparer.OrdinalIgnoreCase) { }
		protected override string GetKeyForItem(MemberMapping member) => member.Token.Name;
		protected override void InsertItem(int index, MemberMapping item)
		{
			if(this.Contains(item.Token.Name))
				throw new DataException($"The specified '{item.Token.Name}' member is duplicated.");

			base.InsertItem(index, item);
		}
	}
	#endregion
}
