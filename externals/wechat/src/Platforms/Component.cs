using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat.Platforms;

public sealed partial class Component
{
	#region 成员字段
	private readonly string _secret;
	private readonly string _token;
	private readonly string _encodingAESKey;
	private readonly IComponentStore _store;
	#endregion

	#region 构造函数
	public Component(Options.ComponentOptions options, IComponentStore store)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(store);
		ArgumentException.ThrowIfNullOrWhiteSpace(options.Name);
		ArgumentException.ThrowIfNullOrWhiteSpace(options.Secret);
		ArgumentException.ThrowIfNullOrWhiteSpace(options.Token);
		ArgumentException.ThrowIfNullOrWhiteSpace(options.EncodingAESKey);

		if(options.Name.IndexOfAny([':', '/', '?', '=', '(', ')']) >= 0 || options.Name.Length > 64 || options.Token.Length > 256)
			throw new ArgumentException(null, nameof(options));

		Span<byte> key = stackalloc byte[32];

		try
		{
			if(options.EncodingAESKey.Length != 43 || !Convert.TryFromBase64String(options.EncodingAESKey + "=", key, out var count) || count != 32 || Convert.ToBase64String(key) != options.EncodingAESKey + "=")
				throw new ArgumentException(null, nameof(options));
		}
		finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(key); }

		this.AppId = options.Name;
		_secret = options.Secret;
		_token = options.Token;
		_encodingAESKey = options.EncodingAESKey;
		_store = store;
	}
	#endregion

	#region 公共属性
	public string AppId { get; }
	#endregion

	#region 公共方法
	public async ValueTask<Receipt> ReceiveAsync(Stream body, string timestamp, string nonce, string signature, CancellationToken cancellation = default)
	{
		var notification = await this.ReadNotificationAsync(body, timestamp, nonce, signature, cancellation);
		return await _store.ReceiveAsync(notification, cancellation);
	}
	#endregion
}
