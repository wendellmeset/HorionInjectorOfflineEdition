using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace HorionInjector
{
	// Token: 0x02000004 RID: 4
	public partial class MainWindow : Window
	{
		// Token: 0x06000008 RID: 8
		[DllImport("kernel32.dll")]
		public static extern IntPtr OpenProcess(IntPtr dwDesiredAccess, bool bInheritHandle, uint processId);

		// Token: 0x06000009 RID: 9
		[DllImport("kernel32.dll")]
		public static extern bool CloseHandle(IntPtr hObject);

		// Token: 0x0600000A RID: 10
		[DllImport("kernel32.dll")]
		public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

		// Token: 0x0600000B RID: 11
		[DllImport("kernel32.dll")]
		public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, char[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

		// Token: 0x0600000C RID: 12
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		// Token: 0x0600000D RID: 13
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		// Token: 0x0600000E RID: 14
		[DllImport("kernel32.dll")]
		public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, ref IntPtr lpThreadId);

		// Token: 0x0600000F RID: 15
		[DllImport("kernel32.dll")]
		public static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

		// Token: 0x06000010 RID: 16
		[DllImport("kernel32.dll")]
		public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, IntPtr dwFreeType);

		// Token: 0x06000011 RID: 17
		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		// Token: 0x06000012 RID: 18
		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		// Token: 0x06000013 RID: 19 RVA: 0x000021DC File Offset: 0x000003DC
		private void Inject(string path)
		{
			if (!File.Exists(path))
			{
				MessageBox.Show("DLL not found, your Antivirus might have deleted it.");
			}
			else if (File.ReadAllBytes(path).Length < 10)
			{
				MessageBox.Show("DLL broken (Less than 10 bytes)");
			}
			else
			{
				this.SetStatus("setting file perms");
				try
				{
					FileInfo fileInfo = new FileInfo(path);
					FileSecurity accessControl = fileInfo.GetAccessControl();
					accessControl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier("S-1-15-2-1"), FileSystemRights.FullControl, InheritanceFlags.None, PropagationFlags.NoPropagateInherit, AccessControlType.Allow));
					fileInfo.SetAccessControl(accessControl);
				}
				catch (Exception)
				{
					MessageBox.Show("Could not set permissions, try running the injector as admin.");
					goto IL_2CC;
				}
				this.SetStatus("finding process");
				Process[] processes = Process.GetProcessesByName("Minecraft.Windows");
				if (processes.Length == 0)
				{
					this.SetStatus("launching minecraft");
					if (Interaction.Shell("explorer.exe shell:appsFolder\\Microsoft.MinecraftUWP_8wekyb3d8bbwe!App", AppWinStyle.MinimizedFocus, false, -1) == 0)
					{
						MessageBox.Show("Failed to launch Minecraft (Is it installed?)");
						goto IL_2CC;
					}
					Task.Run(delegate()
					{
						int num2 = 0;
						while (processes.Length == 0)
						{
							if (++num2 > 200)
							{
								MessageBox.Show("Minecraft launch took too long.");
								return;
							}
							processes = Process.GetProcessesByName("Minecraft.Windows");
							Thread.Sleep(10);
						}
						Thread.Sleep(3000);
					}).Wait();
				}
				Process process = processes.First((Process p) => p.Responding);
				for (int i = 0; i < process.Modules.Count; i++)
				{
					if (process.Modules[i].FileName == path)
					{
						MessageBox.Show("Already injected!");
						goto IL_2CC;
					}
				}
				this.SetStatus("injecting into " + process.Id.ToString());
				IntPtr intPtr = MainWindow.OpenProcess((IntPtr)2035711, false, (uint)process.Id);
				if (intPtr == IntPtr.Zero || !process.Responding)
				{
					MessageBox.Show("Failed to get process handle");
				}
				else
				{
					IntPtr intPtr2 = MainWindow.VirtualAllocEx(intPtr, IntPtr.Zero, (uint)(path.Length + 1), 12288U, 64U);
					IntPtr intPtr3;
					MainWindow.WriteProcessMemory(intPtr, intPtr2, path.ToCharArray(), path.Length, out intPtr3);
					IntPtr procAddress = MainWindow.GetProcAddress(MainWindow.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
					IntPtr intPtr4 = MainWindow.CreateRemoteThread(intPtr, IntPtr.Zero, 0U, procAddress, intPtr2, 0U, ref intPtr3);
					if (intPtr4 == IntPtr.Zero)
					{
						MessageBox.Show("Failed to create remote thread");
					}
					else
					{
						uint num = MainWindow.WaitForSingleObject(intPtr4, 5000U);
						if ((ulong)num == 128UL || (ulong)num == 258UL)
						{
							MainWindow.CloseHandle(intPtr4);
						}
						else
						{
							MainWindow.VirtualFreeEx(intPtr, intPtr2, 0, (IntPtr)32768);
							if (intPtr4 != IntPtr.Zero)
							{
								MainWindow.CloseHandle(intPtr4);
							}
							if (intPtr != IntPtr.Zero)
							{
								MainWindow.CloseHandle(intPtr);
							}
						}
						IntPtr intPtr5 = MainWindow.FindWindow(null, "Minecraft");
						if (intPtr5 == IntPtr.Zero)
						{
							Console.WriteLine("Couldn't get window handle");
						}
						else
						{
							MainWindow.SetForegroundWindow(intPtr5);
						}
					}
				}
			}
			IL_2CC:
			this.SetStatus("done");
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000024D0 File Offset: 0x000006D0
		public MainWindow()
		{
			string text = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "old");
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			this.InitializeComponent();
			this.VersionLabel.Content = string.Format("v{0}.{1}.{2}", this.GetVersion().Major, this.GetVersion().Minor, this.GetVersion().Build);
			this.SetConnectionState(MainWindow.ConnectionState.None);
			Task.Run(delegate()
			{
				for (;;)
				{
					if (!this._done)
					{
						int num = this._ticks + 1;
						this._ticks = num;
						if (num > 12)
						{
							this._ticks = 0;
						}
						string load = this._status + ".";
						if (this._ticks > 4)
						{
							load += ".";
						}
						if (this._ticks > 8)
						{
							load += ".";
						}
						Application.Current.Dispatcher.Invoke(DispatcherPriority.Render, new Action(delegate()
						{
							this.InjectButton.Content = load;
						}));
					}
					Thread.Sleep(100);
				}
			});
			if (!this.CheckConnection())
			{
				MessageBox.Show("Couldn't connect to download server. You can still inject a custom DLL.");
				return;
			}
			this.CheckForUpdate();
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002598 File Offset: 0x00000798
		private void SetStatus(string status)
		{
			if (status == "done")
			{
				this._done = true;
				this._status = string.Empty;
				this._ticks = 0;
				Application.Current.Dispatcher.Invoke(DispatcherPriority.Render, new Action(delegate()
				{
					this.InjectButton.Content = "inject";
				}));
			}
			else
			{
				this._done = false;
				this._status = status;
			}
			Console.WriteLine("[Status] " + status);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002608 File Offset: 0x00000808
		private void SetConnectionState(MainWindow.ConnectionState state)
		{
			this._connectionState = state;
			switch (state)
			{
			case MainWindow.ConnectionState.None:
				this.ConnectionStateLabel.Content = "Not connected";
				this.ConnectionStateLabel.Foreground = Brushes.White;
				return;
			case MainWindow.ConnectionState.Connected:
				this.ConnectionStateLabel.Content = "Connected";
				this.ConnectionStateLabel.Foreground = Brushes.ForestGreen;
				return;
			case MainWindow.ConnectionState.Disconnected:
				this.ConnectionStateLabel.Content = "Disconnected";
				this.ConnectionStateLabel.Foreground = Brushes.Coral;
				return;
			default:
				return;
			}
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002694 File Offset: 0x00000894
		private void InjectButton_Left(object sender, RoutedEventArgs e)
		{
			if (!this._done)
			{
				return;
			}
			this.SetStatus("checking connection");
			string text = Path.Combine(Path.GetTempPath(), "Horion.dll");
			File.Copy("Horion.dll", text, true);
			this.Inject(text);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000026D8 File Offset: 0x000008D8
		private void InjectButton_Right(object sender, MouseButtonEventArgs e)
		{
			if (!this._done)
			{
				return;
			}
			this.SetStatus("selecting DLL");
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "dll files (*.dll)|*.dll",
				RestoreDirectory = true
			};
			if (openFileDialog.ShowDialog().GetValueOrDefault())
			{
				this.Inject(openFileDialog.FileName);
				return;
			}
			this.SetStatus("done");
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002089 File Offset: 0x00000289
		private void ConsoleButton_Click(object sender, MouseButtonEventArgs e)
		{
			if (this.console.IsVisible)
			{
				this.console.Close();
				return;
			}
			this.console.Show();
		}

		// Token: 0x0600001A RID: 26 RVA: 0x0000273C File Offset: 0x0000093C
		private bool CheckConnection()
		{
			bool flag;
			try
			{
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://horion.download");
				httpWebRequest.KeepAlive = false;
				httpWebRequest.Timeout = 1000;
				using (httpWebRequest.GetResponse())
				{
					flag = true;
				}
			}
			catch (Exception)
			{
				flag = false;
			}
			return flag;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000020AF File Offset: 0x000002AF
		private Version GetVersion()
		{
			return Assembly.GetExecutingAssembly().GetName().Version;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000020C0 File Offset: 0x000002C0
		private void CloseWindow(object sender, MouseButtonEventArgs e)
		{
			Application.Current.Shutdown();
		}

		// Token: 0x0600001D RID: 29 RVA: 0x000020CC File Offset: 0x000002CC
		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			base.DragMove();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000020D4 File Offset: 0x000002D4
		private void CheckForUpdate()
		{
			if (Version.Parse(new WebClient().DownloadString("https://horion.download/latest")) > this.GetVersion() && MessageBox.Show("New update available! Do you want to update now?", null, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				this.Update();
			}
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000027A0 File Offset: 0x000009A0
		private void Update()
		{
			string location = Assembly.GetExecutingAssembly().Location;
			try
			{
				Directory.GetAccessControl(Path.GetDirectoryName(location));
			}
			catch (UnauthorizedAccessException)
			{
				MessageBox.Show("Uh oh! The updater has no permission to access the injectors directory!");
				return;
			}
			File.Move(location, Path.ChangeExtension(location, "old"));
			new WebClient().DownloadFile("https://horion.download/bin/HorionInjector.exe", location);
			MessageBox.Show("Updater is done! The injector will now restart.");
			Process.Start(location);
			Application.Current.Shutdown();
		}

		// Token: 0x04000003 RID: 3
		private string _status;

		// Token: 0x04000004 RID: 4
		private bool _done = true;

		// Token: 0x04000005 RID: 5
		private int _ticks;

		// Token: 0x04000006 RID: 6
		private MainWindow.ConnectionState _connectionState;

		// Token: 0x04000007 RID: 7
		private readonly ConsoleWindow console = new ConsoleWindow();

		// Token: 0x02000005 RID: 5
		private enum ConnectionState
		{
			// Token: 0x0400000E RID: 14
			None,
			// Token: 0x0400000F RID: 15
			Connected,
			// Token: 0x04000010 RID: 16
			Disconnected
		}
	}
}
