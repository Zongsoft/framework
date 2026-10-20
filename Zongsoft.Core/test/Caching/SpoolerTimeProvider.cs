using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Caching.Tests;

internal sealed class SpoolerTimeProvider : TimeProvider
{
	private readonly object _sync = new();
	private readonly List<ManualTimer> _timers = [];
	private long _ticks;

	public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
	{
		lock(_sync)
		{
			var timer = new ManualTimer(this, callback, state);
			timer.Change(dueTime, period);
			_timers.Add(timer);
			return timer;
		}
	}

	public void Advance(TimeSpan elapsed)
	{
		List<ManualTimer> pending = [];

		lock(_sync)
		{
			_ticks += elapsed.Ticks;

			foreach(var timer in _timers)
			{
				while(!timer.Disposed && timer.DueTicks <= _ticks)
				{
					pending.Add(timer);
					timer.DueTicks = timer.PeriodTicks > 0 ? timer.DueTicks + timer.PeriodTicks : long.MaxValue;
				}
			}
		}

		foreach(var timer in pending)
			timer.Fire();
	}

	private sealed class ManualTimer(SpoolerTimeProvider owner, TimerCallback callback, object state) : ITimer
	{
		public bool Disposed { get; private set; }
		public long DueTicks { get; set; }
		public long PeriodTicks { get; private set; }

		public bool Change(TimeSpan dueTime, TimeSpan period)
		{
			lock(owner._sync)
			{
				if(this.Disposed)
					return false;

				this.DueTicks = dueTime == Timeout.InfiniteTimeSpan ? long.MaxValue : owner._ticks + dueTime.Ticks;
				this.PeriodTicks = period.Ticks;
				return true;
			}
		}

		public void Fire() => callback(state);

		public void Dispose()
		{
			lock(owner._sync)
				this.Disposed = true;
		}

		public ValueTask DisposeAsync()
		{
			this.Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
