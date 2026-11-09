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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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

		public async ValueTask OnEnlistAsync(Transactions.EnlistmentContext context, CancellationToken cancellation)
		{
			if(GetCompletion(context.Phase, out var completion))
				await _session.RequestCompletionAsync(completion).ConfigureAwait(false);
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
