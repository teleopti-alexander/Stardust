﻿using System.Diagnostics;

namespace Stardust.Node.Diagnostics
{
	internal class TaskToExecuteStopWatch : Stopwatch
	{
		public TaskToExecuteStopWatch(bool startDirectly = true)
		{
			if (startDirectly)
			{
				Start();
			}
		}

		public double GetTotalElapsedTimeInDays()
		{
			StopIfRunning();

			return Elapsed.TotalDays;
		}

		private void StopIfRunning()
		{
			if (IsRunning)
			{
				Stop();
			}
		}

		public double GetTotalElapsedTimeInHours()
		{
			StopIfRunning();

			return Elapsed.TotalHours;
		}

		public double GetTotalElapsedTimeInMinutes()
		{
			StopIfRunning();

			return Elapsed.TotalMinutes;
		}

		public double GetTotalElapsedTimeInSeconds()
		{
			StopIfRunning();

			return Elapsed.TotalSeconds;
		}

		public double GetTotalElapsedTimeInMilliseconds()
		{
			StopIfRunning();

			return Elapsed.TotalMilliseconds;
		}
	}
}