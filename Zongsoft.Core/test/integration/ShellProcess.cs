using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Terminals.Commands;

namespace Zongsoft.Terminals.IntegrationTests;

internal sealed class ShellProcess : IAsyncDisposable
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);
	private readonly string _name = $"zongsoft-shell-{Guid.NewGuid():N}";
	private readonly NamedPipeServerStream _pipe;
	private StreamReader _reader;
	private StreamWriter _writer;
	private Process _child;
	private Task<object> _execution;
	private ITerminal _terminal;

	public ShellProcess() => _pipe = new(_name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
	public bool HasExited => _child.HasExited;

	public async Task<Task<object>> ExecuteAsync(ITerminal terminal, string timeout, CancellationToken cancellation, CancellationToken testCancellation)
	{
		var dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
		if(string.IsNullOrEmpty(dotnet))
			dotnet = "dotnet";

		var text = $"\"\"{dotnet}\" \"{typeof(TestProgram).Assembly.Location}\" --shell-child {_name}\"";
		var command = new ShellCommand();
		var cmdlet = new CommandLine.Cmdlet("shell")
		{
			Arguments = { text },
			Options = { new(CommandLine.CmdletOptionKind.Fully, "timeout", timeout) },
		};
		var context = new CommandContext(terminal.Executor, cmdlet, command, null);
		var returned = new TaskCompletionSource<Task<object>>(TaskCreationOptions.RunContinuationsAsynchronously);
		_terminal = terminal;

		_ = Task.Run(() =>
		{
			try { returned.TrySetResult(command.ExecuteAsync(context, cancellation).AsTask()); }
			catch(Exception exception) { returned.TrySetException(exception); }
		}, testCancellation);

		return _execution = await returned.Task.WaitAsync(_timeout, testCancellation);
	}

	public async Task WaitForChildAsync(CancellationToken cancellation)
	{
		var connecting = _pipe.WaitForConnectionAsync(cancellation);
		var completed = await Task.WhenAny(connecting, _execution).WaitAsync(_timeout, cancellation);

		if(completed == _execution && !_pipe.IsConnected)
			throw new InvalidOperationException($"Shell exited before child connection: {await _execution}; stdout={_terminal.Output}; stderr={_terminal.Error}");

		await connecting.WaitAsync(_timeout, cancellation);
		_reader = new StreamReader(_pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
		_writer = new StreamWriter(_pipe, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };
		var identifier = await _reader.ReadLineAsync(cancellation).AsTask().WaitAsync(_timeout, cancellation);
		_child = Process.GetProcessById(int.Parse(identifier, System.Globalization.CultureInfo.InvariantCulture));

		//子进程通过专用管道报告自身后等待测试指令；持有句柄，清理不重用历史 PID。
		_ = _child.SafeHandle;
	}

	public Task ExitAsync(int code, CancellationToken cancellation) =>
		_writer.WriteLineAsync(code.ToString(System.Globalization.CultureInfo.InvariantCulture).AsMemory(), cancellation);

	public async ValueTask DisposeAsync()
	{
		try
		{
			if(_child != null && !_child.HasExited)
			{
				_child.Kill();
				await _child.WaitForExitAsync().WaitAsync(_timeout);
			}
		}
		finally
		{
			_child?.Dispose();

			if(_writer != null)
				await _writer.DisposeAsync();

			_reader?.Dispose();
			await _pipe.DisposeAsync();
		}
	}

	public static async Task<int> RunChildAsync(string name)
	{
		using var deadline = new CancellationTokenSource(_timeout);
		await using var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
		await pipe.ConnectAsync(deadline.Token);
		using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
		await using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, leaveOpen: true) { AutoFlush = true };

		await writer.WriteLineAsync(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture).AsMemory(), deadline.Token);
		await Console.Out.WriteLineAsync("shell-stdout");
		await Console.Error.WriteLineAsync("shell-stderr");
		var result = await reader.ReadLineAsync(deadline.Token);
		return int.Parse(result, System.Globalization.CultureInfo.InvariantCulture);
	}
}
