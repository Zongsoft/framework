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
using System.Collections.Generic;

namespace Zongsoft.Transitions
{
	/// <summary>
	/// 表示状态机运行参数的选项类。
	/// </summary>
	public class StateMachineOptions
	{
		#region 构造函数
		public StateMachineOptions()
		{
			this.AffiliateTransactionEnabled = true;

			this.OverwriteDestinationStateEnabled = false;
			this.OverwriteIntermediateStateEnabled = true;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 是否关联上下文事务，默认值为真。
		/// </summary>
		/// <remarks>
		///		<para>启用该特性表示当状态机即将完成时会自动创建或关联到上下文事务，并在完成所有状态点的更新后提交当前上下文事务。</para>
		/// </remarks>
		public bool AffiliateTransactionEnabled
		{
			get;
			set;
		}

		/// <summary>
		/// 是否允许覆盖前趋状态机的目标状态。
		/// </summary>
		public bool OverwriteDestinationStateEnabled
		{
			get;
			set;
		}

		/// <summary>
		/// 是否允许覆盖前趋状态机的中间状态。
		/// </summary>
		public bool OverwriteIntermediateStateEnabled
		{
			get;
			set;
		}
		#endregion
	}
}
