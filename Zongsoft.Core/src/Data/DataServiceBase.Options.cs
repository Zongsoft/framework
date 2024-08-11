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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	partial class DataServiceBase<TModel>
	{
		/// <summary>表示数据服务操作选项构建的入口。</summary>
		public static class Options
		{
			/// <summary>表示删除操作的选项。</summary>
			public sealed class Deletion : DataDeleteOptions
			{
				#region 构造函数
				public Deletion(bool allowed = false) => this.Allowed = allowed;
				public Deletion(bool allowed, IEnumerable<KeyValuePair<string, object>> states = null) : base(states) => this.Allowed = allowed;
				#endregion

				#region 公共属性
				/// <summary>获取一个值，指示无论服务是否禁用删除操作，都允许删除操作。</summary>
				public bool Allowed { get; }
				#endregion

				#region 静态方法
				public static Deletion Allow(bool validatorSuppressed = false) => new Deletion(true) { ValidatorSuppressed = validatorSuppressed };
				#endregion
			}

			/// <summary>表示修改操作的选项。</summary>
			public sealed class Updation : DataUpdateOptions
			{
				#region 构造函数
				public Updation(bool allowed = false) => this.Allowed = allowed;
				public Updation(bool allowed, IEnumerable<KeyValuePair<string, object>> states = null) : base(states) => this.Allowed = allowed;
				public Updation(bool allowed, UpdateBehaviors behaviors, IEnumerable<KeyValuePair<string, object>> states = null) : base(behaviors, states) => this.Allowed = allowed;
				#endregion

				#region 公共属性
				/// <summary>获取一个值，指示无论服务是否禁用修改操作，都允许修改操作。</summary>
				public bool Allowed { get; }
				#endregion

				#region 静态方法
				public static Updation Allow(UpdateBehaviors behaviors = UpdateBehaviors.None) => new Updation(true, behaviors);
				public static Updation Allow(bool validatorSuppressed, UpdateBehaviors behaviors = UpdateBehaviors.None) => new Updation(true, behaviors) { ValidatorSuppressed = validatorSuppressed };
				#endregion
			}

			/// <summary>表示增改操作的选项。</summary>
			public sealed class Upsertion : DataUpsertOptions
			{
				#region 构造函数
				public Upsertion(bool allowed = false) => this.Allowed = allowed;
				public Upsertion(bool allowed, IEnumerable<KeyValuePair<string, object>> states = null) : base(states) => this.Allowed = allowed;
				#endregion

				#region 公共属性
				/// <summary>获取一个值，指示无论服务是否禁用增改操作，都允许增改操作。</summary>
				public bool Allowed { get; }
				#endregion

				#region 静态方法
				public static Upsertion Allow(bool validatorSuppressed = false) => new Upsertion(true) { ValidatorSuppressed = validatorSuppressed };
				#endregion
			}

			/// <summary>表示新增操作的选项。</summary>
			public sealed class Insertion : DataInsertOptions
			{
				#region 构造函数
				public Insertion(bool allowed = false) => this.Allowed = allowed;
				public Insertion(bool allowed, IEnumerable<KeyValuePair<string, object>> states = null) : base(states) => this.Allowed = allowed;
				#endregion

				#region 公共属性
				/// <summary>获取一个值，指示无论服务是否禁用新增操作，都允许新增操作。</summary>
				public bool Allowed { get; }
				#endregion

				#region 静态方法
				public static Insertion Allow(bool validatorSuppressed = false, bool sequenceSuppressed = false) =>
				new Insertion(true)
				{
					ValidatorSuppressed = validatorSuppressed,
					SequenceSuppressed = sequenceSuppressed,
				};
				#endregion
			}

			#region 静态方法
			internal static bool Allowed(IDataOptions options)
			{
				if(options == null)
					return false;

				return options switch
				{
					Deletion deletion => deletion.Allowed,
					Updation updation => updation.Allowed,
					Upsertion upsertion => upsertion.Allowed,
					Insertion insertion => insertion.Allowed,
					_ => false,
				};
			}
			#endregion
		}
	}
}
