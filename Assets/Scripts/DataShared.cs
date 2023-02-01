using System;
using System.Collections.Generic;
using System.Text;
using Utility;

public sealed class DataShared
{
	private DataShared() { }

	public static readonly DataShared I = new();

	public string StrategyCurrent;
	public int ScoreCurrent;

	private readonly int LOG_LINES_MAX_I = 20;
	private readonly Queue<string> _log = new();

	public void Log(string message)
	{
		var split = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		for(var index = 0; index < split.Length; index++)
		{
			var line = index == 0 ? split[index] : $"{new string(' ', 2)}{split[index]}";
			_log.Enqueue(line);
			line.Log();
		}
		while(_log.Count > LOG_LINES_MAX_I)
		{
			_log.Dequeue();
		}
	}

	public string RenderVars()
	{
		return new StringBuilder()
			.AppendLine($"02: {StrategyCurrent}")
			.ToString();
	}

	public string RenderLog()
	{
		var builder = new StringBuilder();
		foreach(var line in _log)
		{
			builder.AppendLine(line);
		}
		return builder.ToString();
	}
}
