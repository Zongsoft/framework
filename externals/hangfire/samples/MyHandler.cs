using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Diagnostics;

namespace Zongsoft.Externals.Hangfire.Samples
{
	public class MyHandler : HandlerBase<object>
	{
		private int _count = 0;

		protected override ValueTask OnHandleAsync(object caller, object request, IDictionary<string, object> parameters, CancellationToken cancellation)
		{
			if(request == null)
				request = $"Count:{Interlocked.Increment(ref _count)}";

			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write(request);
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.WriteLine(" " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			Console.ResetColor();

			Logger.Debug("OnHandle the scheduled job.", request);

			return ValueTask.CompletedTask;
		}
	}
}
