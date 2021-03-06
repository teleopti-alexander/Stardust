﻿using Stardust.Node.Extensions;
using Stardust.Node.Interfaces;
using Stardust.Node.Workers;

namespace Stardust.Node.Timers
{
	public class TrySendJobDoneStatusToManagerTimer : TrySendStatusToManagerTimer
	{
		public TrySendJobDoneStatusToManagerTimer(NodeConfiguration nodeConfiguration,
												  JobDetailSender jobDetailSender,
												  IHttpSender httpSender,
												  double interval = 500) : base(nodeConfiguration,
		                                                                          nodeConfiguration.GetManagerJobDoneTemplateUri(),
																				  jobDetailSender,
																				  httpSender,
																				  interval)
		{
		}
	}
}