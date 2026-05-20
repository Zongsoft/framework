using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Diagnostics.Tests;

public class TextFileLoggerTest
{
	[Fact]
	public async Task TestInfoAsync()
	{
		const int COUNT = 512;

		using var context = new LoggerContext();
		using var logger = context.CreateLogger(16);

		await Parallel.ForAsync(0, COUNT, async (index, cancellation) =>
		{
			await logger.LogAsync(new LogEntry(LogLevel.Info, "ConcurrentInfo", GetMessage(index)), cancellation);
		});

		await logger.FlushAsync();

		var content = context.ReadAllText();
		AssertMessages(content, COUNT);
	}

	[Fact]
	public async Task TestErrorAsync()
	{
		const int COUNT = 256;

		using var context = new LoggerContext();
		using var logger = context.CreateLogger(16);

		await Parallel.ForAsync(0, COUNT, async (index, cancellation) =>
		{
			await logger.LogAsync(new LogEntry(LogLevel.Error, "ConcurrentError", GetMessage(index)), cancellation);
		});

		var content = context.ReadAllText();
		AssertMessages(content, COUNT);
	}

	private static string GetMessage(int index) => $"TextFileLoggerTest#{index:D4}";

	private static void AssertMessages(string content, int count)
	{
		Assert.False(string.IsNullOrEmpty(content));

		for(int i = 0; i < count; i++)
		{
			var message = GetMessage(i);
			var first = content.IndexOf(message, StringComparison.Ordinal);

			Assert.True(first >= 0, $"The log message '{message}' was not written.");
			Assert.Equal(-1, content.IndexOf(message, first + message.Length, StringComparison.Ordinal));
		}
	}

	private sealed class LoggerContext : IDisposable
	{
		private readonly string _directory;

		public LoggerContext()
		{
			_directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
			this.FilePath = Path.Combine(_directory, "diagnostics.log");
		}

		public string FilePath { get; }

		public TestTextFileLogger CreateLogger(int capacity) => new(TimeSpan.FromHours(1), capacity, this.FilePath);
		public string ReadAllText() => File.ReadAllText(this.FilePath);

		public void Dispose()
		{
			try
			{
				if(Directory.Exists(_directory))
					Directory.Delete(_directory, true);
			}
			catch { }
		}
	}

	private sealed class TestTextFileLogger : TextFileLogger<LogEntry>, IDisposable
	{
		public TestTextFileLogger(TimeSpan period, int capacity, string filePath) : base(period, capacity, filePath) =>
			this.Formatter = XmlLogFormatter.Default;

		public new ValueTask LogAsync(LogEntry log, CancellationToken cancellation = default) => base.LogAsync(log, cancellation);
		public ValueTask FlushAsync(CancellationToken cancellation = default) => ((ILogger)this).FlushAsync(cancellation);

		public void Dispose() => this.Logging?.Dispose();
	}
}
