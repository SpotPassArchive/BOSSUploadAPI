using Microsoft.Data.Sqlite;
using System.Text;
using Serilog;
using System;

namespace BOSSUploadAPI;

public static class Util
{
	public static string ToHexString(this byte[] bytes, bool upper = false)
	{
		StringBuilder sb = new StringBuilder(bytes.Length * 2);
		string fmt = upper ? "{0:X2}" : "{0:x2}";

		for (int i = 0; i < bytes.Length; i++)
			sb.AppendFormat(fmt, bytes[i]);

		return sb.ToString();
	}

	public static void WriteToLog(this Exception ex)
	{
		Log.Error("==================================================");

		#nullable enable

		Exception? exCur = ex;

		if (ex is SqliteException)
		{
			while (exCur != null)
			{
				Log.Error("Database-releated Exception!");

				foreach (string line in exCur.ToString().Split("\n"))
					Log.Error("{MessageLine}", line);

				exCur = exCur.InnerException;
			}

			return;
		}

		while (exCur != null)
		{
			Log.Error("An exception occurred!");
			Log.Error("Exception Type: {ExceptionType}", exCur.GetType().ToString());
			Log.Error("Exception Message:");

			foreach (string line in exCur.Message.Split("\n"))
				Log.Error("{MessageLine}", line);

			if (exCur.StackTrace != null)
			{
				Log.Error("Stack Trace:");

				foreach (string line in exCur.StackTrace.Split("\n"))
					Log.Error("{StackTraceLine}", line);
			}

			exCur = exCur.InnerException;
		}

		#nullable disable

		Log.Error("==================================================");
	}
}
