using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace Zongsoft.Plugins.IntegrationTests;

internal static partial class TerminalSignalNative
{
	public static Process StartHiddenConsole(string executable, string assembly, string scenario, string directory)
	{
		var command = $"\"{executable}\" \"{assembly}\" --plugin-probe \"{scenario}\" \"{directory}\"";
		var startup = new StartupInfo { Size = (uint)Marshal.SizeOf<StartupInfo>(), Flags = 1, ShowWindow = 0 };
		Check(CreateProcess(null, (command + '\0').ToCharArray(), 0, 0, 0, 0x10, 0, directory, ref startup, out var native));
		using var processHandle = new SafeProcessHandle(native.Process, true);
		using var threadHandle = new SafeWaitHandle(native.Thread, true);
		var process = Process.GetProcessById((int)native.ProcessId);

		//在原生创建句柄仍存活时绑定托管句柄；终止时不通过保存的 PID 重新查找进程。
		_ = process.SafeHandle;
		return process;
	}

	public static void Send(PosixSignal signal)
	{
		if(OperatingSystem.IsWindows())
			Check(GenerateConsoleCtrlEvent(signal == PosixSignal.SIGINT ? 0U : 1U, 0));
		else if(Kill(Environment.ProcessId, signal switch { PosixSignal.SIGINT => 2, PosixSignal.SIGQUIT => 3, PosixSignal.SIGTERM => 15, _ => throw new ArgumentOutOfRangeException(nameof(signal)) }) != 0)
			throw new Win32Exception(Marshal.GetLastPInvokeError());
	}

	public static unsafe void WriteInput(string text)
	{
		if(!OperatingSystem.IsWindows())
		{
			Console.WriteLine("\nINPUT:" + text);
			return;
		}

		text += '\r';
		var records = new InputRecord[text.Length * 2];

		for(var index = 0; index < text.Length; index++)
		{
			records[index * 2] = new InputRecord
			{
				EventType = 1,
				KeyDown = 1,
				Repeat = 1,
				VirtualKey = text[index] == '\r' ? (ushort)13 : (ushort)0,
				Character = text[index],
			};
			records[index * 2 + 1] = records[index * 2];
			records[index * 2 + 1].KeyDown = 0;
		}

		fixed(InputRecord* pointer = records)
			Check(WriteConsoleInput(GetStdHandle(-10), pointer, (uint)records.Length, out _));
	}

	private static void Check(int result)
	{
		if(result == 0)
			throw new Win32Exception(Marshal.GetLastPInvokeError());
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct StartupInfo
	{
		public uint Size;
		public nint Reserved, Desktop, Title;
		public uint X, Y, Width, Height, ColumnCount, RowCount, FillAttribute, Flags;
		public ushort ShowWindow, ReservedSize;
		public nint ReservedBytes, Input, Output, Error;
	}

	[StructLayout(LayoutKind.Explicit, Size = 20)]
	private struct InputRecord
	{
		[FieldOffset(0)] public ushort EventType;
		[FieldOffset(4)] public int KeyDown;
		[FieldOffset(8)] public ushort Repeat;
		[FieldOffset(10)] public ushort VirtualKey;
		[FieldOffset(14)] public char Character;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct ProcessInfo
	{
		public nint Process, Thread;
		public uint ProcessId, ThreadId;
	}

	[LibraryImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
	private static partial int CreateProcess(string application, char[] command, nint processAttributes, nint threadAttributes, int inheritHandles, uint flags, nint environment, string directory, ref StartupInfo startup, out ProcessInfo process);
	[LibraryImport("kernel32.dll", SetLastError = true)]
	private static partial int GenerateConsoleCtrlEvent(uint signal, uint group);
	[LibraryImport("kernel32.dll")]
	private static partial nint GetStdHandle(int id);
	[LibraryImport("kernel32.dll", EntryPoint = "WriteConsoleInputW", SetLastError = true)]
	private static unsafe partial int WriteConsoleInput(nint input, InputRecord* records, uint count, out uint written);
	[LibraryImport("libc", EntryPoint = "kill", SetLastError = true)]
	private static partial int Kill(int process, int signal);
}
