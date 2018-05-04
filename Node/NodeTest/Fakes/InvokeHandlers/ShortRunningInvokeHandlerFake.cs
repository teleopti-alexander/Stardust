﻿using System;
using System.Collections.Generic;
using System.Threading;
using Stardust.Node.Interfaces;

namespace NodeTest.Fakes.InvokeHandlers
{
	internal class ShortRunningInvokeHandlerFake : IInvokeHandler
	{
		public void Invoke(object query,
			CancellationTokenSource cancellationTokenSource,
			Action<string> progressCallback,
			ref IEnumerable<object> returnObjects)
		{
			progressCallback("Shortrunning: waiting 3 seconds");
			Thread.Sleep(TimeSpan.FromSeconds(3));
		}
	}
}