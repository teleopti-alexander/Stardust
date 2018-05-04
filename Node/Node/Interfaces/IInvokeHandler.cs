﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Stardust.Node.Interfaces
{
	public interface IInvokeHandler
	{
		void Invoke(object query,
			CancellationTokenSource cancellationTokenSource,
			Action<string> progressCallback,
			ref IEnumerable<object> returnObjects);
	}
}