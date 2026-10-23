using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Wechat;

public partial class Authority
{
	/// <summary>当前商户快照的可信微信材料；公钥模式不自动下载，证书模式只接受已有可信材料签名的更新。</summary>
	public sealed class Verifier
	{
		#region 成员字段
		private readonly object _sync = new();
		private Dictionary<string, VerificationKey> _keys;
		private Task _refresh;
		private DateTime _refreshed = DateTime.UtcNow;
		private DateTime _retry;
		#endregion

		#region 构造函数
		public Verifier(VerificationMode mode, IEnumerable<VerificationKey> keys, string encryptionKeyId = null)
		{
			if(mode != VerificationMode.PublicKey && mode != VerificationMode.PlatformCertificate)
				throw new ArgumentOutOfRangeException(nameof(mode));

			ArgumentNullException.ThrowIfNull(keys);
			encryptionKeyId = string.IsNullOrEmpty(encryptionKeyId) ? null : encryptionKeyId;
			var items = new Dictionary<string, VerificationKey>(StringComparer.Ordinal);

			foreach(var key in keys)
			{
				if(key == null || key.Mode != mode || !items.TryAdd(key.Identifier, key))
					throw new ArgumentException(null, nameof(keys));
			}

			if(items.Count == 0)
				throw new ArgumentException(null, nameof(keys));
			if(mode == VerificationMode.PlatformCertificate && encryptionKeyId != null)
				throw new ArgumentException(null, nameof(encryptionKeyId));
			if(encryptionKeyId != null && !items.ContainsKey(encryptionKeyId))
				throw new ArgumentException(null, nameof(encryptionKeyId));
			if(mode == VerificationMode.PublicKey && items.Count > 1 && encryptionKeyId == null)
				throw new ArgumentException(null, nameof(encryptionKeyId));

			this.Mode = mode;
			this.EncryptionKeyId = encryptionKeyId;
			_keys = items;
		}
		#endregion

		#region 公共属性
		public VerificationMode Mode { get; }
		public string EncryptionKeyId { get; }
		#endregion

		#region 内部方法
		internal Verifier Copy() => new(this.Mode, Volatile.Read(ref _keys).Values, this.EncryptionKeyId);
		internal VerificationKey Find(string identifier)
		{
			var keys = Volatile.Read(ref _keys);
			identifier ??= this.EncryptionKeyId;
			if(!string.IsNullOrEmpty(identifier))
				return keys.TryGetValue(identifier, out var key) && key.Validity.IsValid() ? key : null;

			return keys.Values.Where(key => key.Validity.IsValid()).OrderByDescending(key => key.Validity.Start).ThenByDescending(key => key.Identifier, StringComparer.Ordinal).FirstOrDefault();
		}

		internal async ValueTask<VerificationKey> GetAsync(IAuthority authority, string identifier, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();
			var key = this.Find(identifier);

			if(this.Mode == VerificationMode.PublicKey)
				return key;

			bool fresh;

			lock(_sync)
				fresh = _refreshed > DateTime.UtcNow.AddHours(-10);

			if(key != null && fresh && key.Validity.Final > DateTime.UtcNow.AddHours(36))
				return key;

			await this.RefreshAsync(authority, cancellation);
			return this.Find(identifier);
		}

		internal async ValueTask RefreshAsync(IAuthority authority, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if(this.Mode != VerificationMode.PlatformCertificate)
				return;

			Task pending;

			lock(_sync)
			{
				if(_refresh != null && (!_refresh.IsCompleted || DateTime.UtcNow < _retry))
					pending = _refresh;
				else
				{
					_retry = DateTime.UtcNow.AddMinutes(1);
					pending = _refresh = this.RefreshCoreAsync(authority);
					_ = pending.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
				}
			}

			//调用方取消只终止自身等待，共用刷新由有界 HTTP 超时结束。
			await pending.WaitAsync(cancellation);
		}
		#endregion

		#region 私有方法
		private async Task RefreshCoreAsync(IAuthority authority)
		{
			if(this.Find(null) == null)
				throw new CryptographicException();

			var downloaded = await CertificateUtility.DownloadAsync(authority);
			var previousKeys = Volatile.Read(ref _keys);
			var keys = new Dictionary<string, VerificationKey>(StringComparer.Ordinal);

			foreach(var key in downloaded)
			{
				if(previousKeys.TryGetValue(key.Identifier, out var previous) && !previous.HasSameKey(key))
					throw new CryptographicException();

				keys[key.Identifier] = key;
			}

			foreach(var key in keys.Values.Where(key => key.Validity.Final <= DateTime.UtcNow).ToArray())
				keys.Remove(key.Identifier);

			if(!keys.Values.Any(key => key.Validity.IsValid()))
				throw new CryptographicException();

			lock(_sync)
			{
				Volatile.Write(ref _keys, keys);
				_refreshed = DateTime.UtcNow;
			}
		}
		#endregion
	}
}
