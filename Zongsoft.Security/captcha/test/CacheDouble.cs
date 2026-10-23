using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Zongsoft.Caching;

namespace Zongsoft.Security.Captcha.Tests;

public class CacheDouble : DispatchProxy
{
	public ConcurrentQueue<(string Method, object[] Arguments)> Calls { get; } = new();
	public Func<string, CancellationToken, ValueTask<object>> Read { get; set; }
	public Func<string, object, TimeSpan, CacheRequisite, CancellationToken, ValueTask<bool>> Write { get; set; }
	public Func<string, (bool Removed, object Value)> Take { get; set; }
	public Func<string, CancellationToken, ValueTask<bool>> Delete { get; set; }

	public static AuthencodeCaptcha Create(out CacheDouble recorder, out CacheProvider provider)
	{
		var cache = Create<IDistributedCache, CacheDouble>();
		recorder = (CacheDouble)cache;
		provider = new CacheProvider(cache);
		return new AuthencodeCaptcha(provider);
	}

	protected override object Invoke(MethodInfo targetMethod, object[] args)
	{
		this.Calls.Enqueue((targetMethod.Name, (object[])args.Clone()));

		if(targetMethod.Name == nameof(IDistributedCache.GetValueAsync) && !targetMethod.IsGenericMethod)
			return Box(this.Read((string)args[0], (CancellationToken)args[1]));

		if(targetMethod.Name == nameof(IDistributedCache.SetValueAsync) && args.Length == 5)
			return Box(this.Write((string)args[0], args[1], (TimeSpan)args[2], (CacheRequisite)args[3], (CancellationToken)args[4]));

		if(targetMethod.Name == nameof(IDistributedCache.Remove) && args.Length == 2)
		{
			var result = this.Take((string)args[0]);
			args[1] = result.Value;
			return result.Removed;
		}

		if(targetMethod.Name == nameof(IDistributedCache.RemoveAsync) && args[0] is string key)
			return Box(this.Delete(key, (CancellationToken)args[1]));

		throw new NotSupportedException(targetMethod.Name);
	}

	private static object Box<T>(ValueTask<T> value) => value;
}

public sealed class CacheProvider(IDistributedCache cache) : IServiceProvider, Zongsoft.Services.IServiceProvider<IDistributedCache>
{
	public int Resolutions { get; private set; }

	public object GetService(Type serviceType)
	{
		this.Resolutions++;
		return serviceType == typeof(Zongsoft.Services.IServiceProvider<IDistributedCache>) ? this : null;
	}

	IDistributedCache Zongsoft.Services.IServiceProvider<IDistributedCache>.GetService(string name) => cache;
}
