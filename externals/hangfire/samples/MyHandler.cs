using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Diagnostics;

namespace Zongsoft.Externals.Hangfire.Samples
{
	public class MyHandler : HandlerBase<object>
	{
		private int _count = 0;

		public override OperationResult Handle(object caller, object parameter)
		{
			if(parameter == null)
				parameter = $"Count:{Interlocked.Increment(ref _count)}";

			Console.ForegroundColor = ConsoleColor.DarkYellow;
			Console.Write(parameter);
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.WriteLine(" " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			Console.ResetColor();

			Logger.Debug("OnHandle the scheduled job.", parameter);
			return OperationResult.Success();
		}

		public override ValueTask<OperationResult> HandleAsync(object caller, object parameter, CancellationToken cancellation = default)
		{
			return ValueTask.FromResult(this.Handle(caller, parameter));
		}
	}
}
