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
	public class DistributedLock : IDistributedLock
	{
		#region 私有变量
		private IDistributedLockManager _manager;
		#endregion

		#region 构造函数
		public DistributedLock(IDistributedLockManager manager, string key, byte[] token, DateTime expiry)
		{
			_manager = manager;
			this.Key = key;
			this.Token = token;
			this.Expiry = expiry.Kind == DateTimeKind.Utc ? expiry : expiry.ToUniversalTime();
		}
		#endregion

		#region 公共属性
		public string Key { get; }
		public byte[] Token { get; }
		public DateTime Expiry { get; }

		public bool IsExpired => DateTime.UtcNow >= this.Expiry;
		public bool IsLocked => Volatile.Read(ref _manager) != null && DateTime.UtcNow < this.Expiry;
		public bool IsUnlocked => Volatile.Read(ref _manager) == null || DateTime.UtcNow >= this.Expiry;
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
				var task = this.UnlockAsync();

				if(task.IsCompletedSuccessfully)
					return;

				task.AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			}
		}

		public async ValueTask DisposeAsync()
		{
			var task = this.UnlockAsync();

			if(task.IsCompletedSuccessfully)
				return;

			await AwaitUnlock(task);

			static async ValueTask<bool> AwaitUnlock(ValueTask<bool> task)
			{
				return await task;
			}
		}

		public ValueTask<bool> UnlockAsync(CancellationToken cancellation = default)
		{
			if(_manager != null)
			{
				var manager = Interlocked.Exchange(ref _manager, null);

				if(manager != null)
					return manager.ReleaseAsync(this.Key, this.Token, cancellation);
			}

			return default;
		}
		#endregion
	}
}
