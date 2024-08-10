using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Diagnostics;

namespace Zongsoft.Externals.Hangfire.Samples
{
	public class MyHandler : HandlerBase<object>
	{
		private int _count = 0;

		protected override ValueTask OnHandleAsync(object request, Parameters parameters, CancellationToken cancellation)
		{
			if(request == null)
				request = $"Count:{Interlocked.Increment(ref _count)}";

			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write(request);
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.WriteLine(" " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			Console.ResetColor();

			Logger.GetLogger(this).Debug("OnHandle the scheduled job.", request);

			return ValueTask.CompletedTask;
		}
	}
}
