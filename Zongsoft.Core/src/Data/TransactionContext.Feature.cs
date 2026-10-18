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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Data.Transactions;

namespace Zongsoft.Data;

partial class TransactionContext
{
	#region 成员字段
	private Feature _feature;
	#endregion

	#region 内部方法
	/// <summary>获取当前根事务共享的运行时条件特性。</summary>
	/// <returns>绑定当前根事务的特性集合。</returns>
	internal IEnumerable<IFeature> GetFeatures() => [_feature];
	#endregion

	#region 分部方法
	/// <summary>初始化根事务的条件特性，嵌套事务复用相同实例。</summary>
	partial void OnInitialized() => _feature = this.Parent?.Root._feature ?? new(this);
	#endregion

	#region 嵌套子类
	/// <summary>以异步条件表达根事务是否成功提交。</summary>
	private sealed class Feature : IFeature, IPredication
	{
		/// <summary>保存根事务的最终条件结果。</summary>
		private readonly TaskCompletionSource<bool> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>在根事务进入调用环境前订阅完成通知。</summary>
		/// <param name="context">条件所绑定的根事务上下文。</param>
		public Feature(TransactionContext context) => context.Completed += this.OnCompleted;

		/// <summary>根据已绑定根事务的最终状态求值条件。</summary>
		/// <param name="argument">条件参数；事务条件仅取决于根事务状态。</param>
		/// <param name="cancellation">只取消本次求值，不影响事务及其它求值。</param>
		/// <returns>成功提交返回 <see langword="true"/>，其它最终状态返回 <see langword="false"/>；事务未完成时保持等待。</returns>
		public ValueTask<bool> PredicateAsync(object argument, CancellationToken cancellation = default) => this.PredicateAsync(argument, null, cancellation);

		/// <summary>根据已绑定根事务的最终状态求值条件。</summary>
		/// <param name="argument">条件参数；事务条件仅取决于根事务状态。</param>
		/// <param name="parameters">附加参数；不参与事务条件求值。</param>
		/// <param name="cancellation">只取消本次求值，不影响事务及其它求值。</param>
		/// <returns>成功提交返回 <see langword="true"/>，其它最终状态返回 <see langword="false"/>；事务未完成时保持等待。</returns>
		public ValueTask<bool> PredicateAsync(object argument, Parameters parameters, CancellationToken cancellation = default) => new(_completed.Task.WaitAsync(cancellation));

		/// <summary>发布事务条件的最终结果并解除订阅。</summary>
		/// <param name="sender">已经完成的根事务上下文。</param>
		/// <param name="args">事务完成事件参数。</param>
		private void OnCompleted(object sender, EventArgs args)
		{
			var context = (TransactionContext)sender;
			context.Completed -= this.OnCompleted;
			_completed.TrySetResult(context.Status == TransactionStatus.Committed);
		}
	}
	#endregion
}
