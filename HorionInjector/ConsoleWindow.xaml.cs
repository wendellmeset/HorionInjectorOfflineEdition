using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;

namespace HorionInjector
{
	// Token: 0x02000003 RID: 3
	public partial class ConsoleWindow : Window
	{
		// Token: 0x06000004 RID: 4 RVA: 0x00002063 File Offset: 0x00000263
		public ConsoleWindow()
		{
			this.InitializeComponent();
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002071 File Offset: 0x00000271
		protected override void OnClosing(CancelEventArgs e)
		{
			e.Cancel = true;
			base.Hide();
		}
	}
}
