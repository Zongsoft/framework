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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Distributing
{
	public abstract class DistributedLockBase<TManager> : IDistributedLock, IDisposable, IAsyncDisposable where TManager : class, IDistributedLockManager
	{
		#region 私有变量
		private TManager _manager;
		private DateTime? _heldTime;
		#endregion

		#region 构造函数
		protected DistributedLockBase(TManager manager, string key, byte[] token, TimeSpan expiry, bool isHeld)
		{
			if(manager == null)
				throw new ArgumentNullException(nameof(manager));
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));
            if(token == null || token.Length == 0)
				throw new ArgumentNullException(nameof(token));

			_manager = manager;
			_heldTime = isHeld ? DateTime.UtcNow : null;

			this.Key = key;
			this.Token = token;
			this.Expiry = expiry;
		}
		#endregion

		#region 公共属性
		public string Key { get; }
		public byte[] Token { get; }
		public TimeSpan Expiry { get; }
		protected TManager Manager => _manager;

		public bool IsExpired => _heldTime.HasValue && DateTime.UtcNow >= _heldTime.Value.Add(this.Expiry);
		public bool IsHeld => _heldTime.HasValue && Volatile.Read(ref _manager) != null;
		public bool IsUnheld => !this.IsHeld;
		public bool IsLocked => this.IsHeld && !this.IsExpired;
		public bool IsUnlocked => !this.IsLocked;
		#endregion

		#region 公共方法
		public virtual async ValueTask EnterAsync(CancellationToken cancellation = default)
		{
			//如果当前锁已经释放则抛出异常
			var manager = Volatile.Read(ref _manager) ?? throw new InvalidOperationException($"The distributed lock has been released.");

			while(this.IsUnheld)
			{
				//获取当前锁的剩余有效期
				var expiry = await manager.GetExpiryAsync(this.Key, cancellation);

				if(expiry.HasValue && expiry.Value > TimeSpan.Zero)
					await Task.Delay(expiry.Value, cancellation);

				//进入分布式锁临界区，进入成功则更新持有时间
				if(await this.OnEnterAsync(cancellation))
					_heldTime = DateTime.UtcNow;
			}
		}

		protected abstract ValueTask<bool> OnEnterAsync(CancellationToken cancellation);
		#endregion

		#region 释放方法
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				var task = this.ExitAsync();

				if(task.IsCompletedSuccessfully)
					return;

				task.AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			}
		}

		public async ValueTask DisposeAsync()
		{
			await this.DisposeAsync(true);
			GC.SuppressFinalize(this);
		}

		protected virtual async ValueTask DisposeAsync(bool disposing)
		{
			if(disposing)
			{
				var task = this.ExitAsync();

				if(task.IsCompletedSuccessfully)
					return;

				await AwaitUnlock(task);
			}

			static async ValueTask<bool> AwaitUnlock(ValueTask<bool> task) => await task;
		}

		public ValueTask<bool> ExitAsync(CancellationToken cancellation = default)
		{
			if(_manager != null)
			{
				var manager = Interlocked.Exchange(ref _manager, null);

				if(manager != null)
					return manager.ReleaseAsync(this.Key, this.Token, cancellation);
			}

			return ValueTask.FromResult(false);
		}
		#endregion

		#region 重写方法
		public override string ToString() => this.Token == null ?
			$"{this.Key}({this.Expiry})" :
			$"{this.Key}:{Convert.ToHexString(this.Token)}({this.Expiry})";
		#endregion
	}
}
