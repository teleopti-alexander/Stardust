﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using log4net;
using Stardust.Node.Extensions;
using Stardust.Node.Interfaces;
using Stardust.Node.Workers;
using Timer = System.Timers.Timer;

namespace Stardust.Node.Timers
{
	public class PingToManagerTimer : Timer
	{
		private static readonly ILog Logger = LogManager.GetLogger(typeof (PingToManagerTimer));

		public PingToManagerTimer(NodeConfiguration nodeConfiguration,
		                          IHttpSender httpSender) : base(nodeConfiguration.PingToManagerSeconds*1000)
		{
			_cancellationTokenSource = new CancellationTokenSource();

			_nodeConfiguration = nodeConfiguration;
			_httpSender = httpSender;
			_whoAmI = nodeConfiguration.CreateWhoIAm(Environment.MachineName);

			Elapsed += OnTimedEvent;
		}

		private readonly string _whoAmI;
		private readonly NodeConfiguration _nodeConfiguration;
		private readonly IHttpSender _httpSender;
		private readonly CancellationTokenSource _cancellationTokenSource;

		protected override void Dispose(bool disposing)
		{
			if (_cancellationTokenSource != null &&
			    !_cancellationTokenSource.IsCancellationRequested)
			{
				_cancellationTokenSource.Cancel();
			}
			base.Dispose(disposing);
		}


		private async Task<HttpResponseMessage> SendPing(Uri nodeAddress,
		                                                 Uri callbackToManagerUri,
		                                                 CancellationToken cancellationToken)
		{
			var httpResponseMessage = await _httpSender.PostAsync(callbackToManagerUri,
			                                                     nodeAddress,
			                                                     cancellationToken);
			return httpResponseMessage;
		}

		private async void OnTimedEvent(object sender, ElapsedEventArgs e)
		{
			try
			{
				await SendPing(_nodeConfiguration.BaseAddress,
							   _nodeConfiguration.GetManagerNodeHeartbeatUri(),
				               _cancellationTokenSource.Token);
			}
			catch
			{
				Logger.InfoWithLineNumber(_whoAmI + ": Heartbeat failed. Is the manager up and running?");
			}
		}
	}
}