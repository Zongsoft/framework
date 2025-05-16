using System;

using Zongsoft.Caching;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Samples;

internal class Program
{
	static void Main(string[] args)
	{
		const int FREQUENCY  = 1;
		const int EXPIRATION = 30;
		const int LIMIT      = 5;

		using var cache = new MemoryCache(TimeSpan.FromSeconds(FREQUENCY), LIMIT);
		using var scanner = new MemoryCacheScanner(cache);

		cache.Limited += Cache_Limited;
		cache.Evicted += Cache_Evicted;

		Terminal.WriteLine(CommandOutletColor.Gray, new string('·', 60));
		Terminal.Write(CommandOutletColor.DarkMagenta, $"Scan Frequency: ");
		Terminal.Write(CommandOutletColor.DarkYellow, FREQUENCY.ToString());
		Terminal.Write(CommandOutletColor.DarkGray, "(s), ");

		Terminal.Write(CommandOutletColor.DarkMagenta, $"Expiration: ");
		Terminal.Write(CommandOutletColor.DarkYellow, EXPIRATION.ToString());
		Terminal.Write(CommandOutletColor.DarkGray, "(s), ");

		Terminal.Write(CommandOutletColor.DarkMagenta, $"Count Limit: ");
		Terminal.Write(CommandOutletColor.DarkYellow, LIMIT.ToString());
		Terminal.WriteLine(CommandOutletColor.DarkGray, ".");
		Terminal.WriteLine(CommandOutletColor.Gray, new string('·', 60));

		Terminal.WriteLine();

		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `exit`  to quit the program.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `start` to start the cache scanner.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `stop`  to stop the cache scanner.");
		Terminal.Write(CommandOutletColor.Red, "Tips: ");
		Terminal.WriteLine(CommandOutletColor.DarkGray, "Input other text is added to the cache as value.");
		Terminal.WriteLine();

		var count = 0;
		var text = string.Empty;

		while(true)
		{
			text = Console.ReadLine().Trim();

			switch(text)
			{
				case "exit":
					return;
				case "start":
				case "restart":
					scanner.Start();
					Terminal.WriteLine(CommandOutletColor.DarkGreen, "The memory cache scanner has started.");
					break;
				case "stop":
					scanner.Stop();
					Terminal.WriteLine(CommandOutletColor.DarkMagenta, "The memory cache scanner has stopped.");
					break;
				default:
					if(!string.IsNullOrEmpty(text))
						cache.SetValue($"Key#{++count}", text, TimeSpan.FromSeconds(EXPIRATION), Now);
					break;
			}
		}
	}

	private static void Cache_Evicted(object sender, CacheEvictedEventArgs e)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.Magenta, "** Evicted **\t")
			.Append(CommandOutletColor.DarkGreen, Now + ' ')
			.Append(CommandOutletColor.Blue, $"[{e.Reason}] ")
			.Append(CommandOutletColor.DarkYellow, e.Key.ToString())
			.Append(CommandOutletColor.DarkGray, "=")
			.Append(CommandOutletColor.DarkYellow, e.Value?.ToString());

		if(e.State != null)
			content
				.Append(CommandOutletColor.DarkGray, " (")
				.Append(CommandOutletColor.Cyan, e.State.ToString())
				.Append(CommandOutletColor.DarkGray, ")");

		Terminal.WriteLine(content);
	}

	private static void Cache_Limited(object sender, CacheLimitedEventArgs e)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.Magenta, "** Limited **\t")
			.Append(CommandOutletColor.DarkYellow, e.Limit.ToString())
			.Append(CommandOutletColor.DarkGray, "/")
			.Append(CommandOutletColor.DarkYellow, e.Count.ToString());

		Terminal.WriteLine(content);

		//清空缓存
		((MemoryCache)sender).Clear();
	}

	private static string Now => DateTime.Now.ToString("HH:mm:ss.ffffff");
}
