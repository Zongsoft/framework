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
using System.Collections.Generic;

namespace Zongsoft.Data.Common
{
	public class EntityPopulator : IDataPopulator
	{
		#region 成员字段
		private readonly Type _type;
		private readonly IEnumerable<PopulateToken> _tokens;
		#endregion

		#region 构造函数
		internal EntityPopulator(Type type, IEnumerable<PopulateToken> tokens)
		{
			_type = type ?? throw new ArgumentNullException(nameof(type));
			_tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
		}
		#endregion

		#region 公共方法
		public object Populate(IDataRecord record)
		{
			return this.Populate(record, this.GetCreator(_type), _tokens);
		}
		#endregion

		#region 私有方法
		private object Populate(IDataRecord record, Func<IDataRecord, object> creator, IEnumerable<PopulateToken> tokens)
		{
			object entity = null;

			foreach(var token in tokens)
			{
				if(token.Ordinal >= 0)
				{
					if(entity == null)
						entity = creator(record);

					token.Member.Populate(ref entity, record, token.Ordinal);
				}
				else if(this.CanPopulate(record, token))
				{
					if(entity == null)
						entity = creator(record);

					token.Member.SetValue(entity, this.Populate(record, this.GetCreator(token.Member.Type), token.Tokens));
				}
			}

			return entity;
		}
		#endregion

		#region 虚拟方法
		protected virtual Func<IDataRecord, object> GetCreator(Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(type.IsAbstract)
				return record => Model.Build(type);

			return record => System.Activator.CreateInstance(type);
		}
		#endregion

		#region 私有方法
		private bool CanPopulate(IDataRecord record, PopulateToken token)
		{
			for(int i = 0; i < token.Keys.Length; i++)
			{
				if(token.Keys[i] < 0 || record.IsDBNull(token.Keys[i]))
					return false;
			}

			return true;
		}
		#endregion

		#region 嵌套子类
		internal struct PopulateToken
		{
			#region 公共字段
			public readonly int Ordinal;
			public readonly EntityMember Member;
			public readonly Metadata.IDataEntity Entity;
			public readonly ICollection<PopulateToken> Tokens;
			public readonly int[] Keys;
			#endregion

			#region 构造函数
			public PopulateToken(Metadata.IDataEntity entity, EntityMember member, int ordinal)
			{
				this.Ordinal = ordinal;
				this.Entity = entity;
				this.Member = member;
				this.Tokens = null;
				this.Keys = null;
			}

			public PopulateToken(Metadata.IDataEntity entity, EntityMember member)
			{
				this.Ordinal = -1;
				this.Entity = entity;
				this.Member = member;
				this.Tokens = new List<PopulateToken>();
				this.Keys = new int[entity.Key.Length];

				for(int i = 0; i < this.Keys.Length; i++)
				{
					this.Keys[i] = -1;
				}
			}
			#endregion

			#region 重写方法
			public override string ToString() => this.Tokens == null ?
				$"[{this.Ordinal}]{this.Member}" :
				$"[{this.Ordinal}]{this.Member}({this.Tokens.Count})";
			#endregion
		}
		#endregion
	}
}
