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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Common;

partial class DataSession
{
	private sealed class Enlistment(DataSession session) : Transactions.IEnlistment
	{
		private readonly DataSession _session = session;

		public void OnEnlist(Transactions.EnlistmentContext context)
		{
			if(GetCompletion(context.Phase, out var completion))
				_session.CompleteAndWait(completion);
		}

		public async ValueTask OnEnlistAsync(Transactions.EnlistmentContext context, CancellationToken cancellation)
		{
			if(GetCompletion(context.Phase, out var completion))
				await _session.CompleteAndWaitAsync(completion).ConfigureAwait(false);
		}

		private static bool GetCompletion(Transactions.EnlistmentPhase phase, out CompletionKind completion)
		{
			switch(phase)
			{
				case Transactions.EnlistmentPhase.Commit:
					completion = CompletionKind.Commit;
					return true;
				case Transactions.EnlistmentPhase.Abort:
				case Transactions.EnlistmentPhase.Rollback:
					completion = CompletionKind.Rollback;
					return true;
				default:
					completion = CompletionKind.None;
					return false;
			}
		}
	}
}
