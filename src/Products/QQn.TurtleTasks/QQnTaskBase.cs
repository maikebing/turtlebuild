﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace QQn.TurtleTasks
{
    static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool IsGUIThread(bool bConvert);
    }
	public abstract class QQnTaskBase : Task
	{
		public string[] ApplySecondaryValue(ITaskItem[] values, string valueName, ITaskItem[] primaryValues)
		{
			if (primaryValues == null)
				throw new ArgumentNullException("primaryValues");

			if (values == null)
				values = new ITaskItem[0];

			string[] vals = new string[primaryValues.Length];

			if (values.Length <= 1)
			{
				string baseValue = (values.Length == 1) ? values[0].ItemSpec : null;

				for (int i = 0; i < primaryValues.Length; i++)
				{
					vals[i] = baseValue ?? ((valueName != null) ? primaryValues[i].GetMetadata(valueName) : null);
				}
			}
			else if (values.Length != primaryValues.Length)
				throw new ArgumentException(string.Format("The number of values in {0} must be 0, 1 or the number of primary items", valueName), "values");
			else
				for (int i = 0; i < values.Length; i++)
				{
					vals[i] = values[i].ItemSpec;
				}

			return vals;
		}

		static bool? _isNonInteractive;
		public static bool IsNonInteractive
		{
			get
			{
				if (!_isNonInteractive.HasValue)
				{
					if (!Environment.UserInteractive)
						_isNonInteractive = false;
					else
					{
						Process p = Process.GetCurrentProcess();
						bool hasMainWindow = false;
						try
						{
							if (p.MainWindowHandle != IntPtr.Zero)
								hasMainWindow = true;
						}
						catch { }

						_isNonInteractive = !hasMainWindow;
					}
				}

				return _isNonInteractive.Value;
			}
		}

		public static int WaitAnyWithUI(WaitHandle[] handles, TimeSpan timeout, bool exitContext)
		{
			if (timeout < new TimeSpan(0))
				throw new ArgumentOutOfRangeException();

			if (IsNonInteractive || !NativeMethods.IsGUIThread(false))
				return WaitHandle.WaitAny(handles, timeout, exitContext);
			else
			{
				// We are running inside VS.Net, which blocks the UI thread for us.. Workaround it by pumping messages
				// Informally confirmed by MS for VS 2005-2008, so perhaps it is resolved after VS 2008
				DateTime end = DateTime.Now + timeout;

				do
				{
					int n = WaitHandle.WaitAny(handles, 1, exitContext);

					if (n < 0 || (n == WaitHandle.WaitTimeout))
					{
						DoEvents();
					}
					else
						return n;
				}
				while (DateTime.Now < end);

				return WaitHandle.WaitTimeout;
			}
		}

		private static void DoEvents()
		{
            System.Windows.Forms.Application.DoEvents();
		}
	}
}
