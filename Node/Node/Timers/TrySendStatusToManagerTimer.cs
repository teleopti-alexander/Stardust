﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using log4net;
using Stardust.Node.Extensions;
using Stardust.Node.Helpers;
using Stardust.Node.Interfaces;
using Timer = System.Timers.Timer;

namespace Stardust.Node.Timers
{
	public class TrySendStatusToManagerTimer : Timer
	{
		private static readonly ILog Logger =
			LogManager.GetLogger(typeof (TrySendStatusToManagerTimer));

		public TrySendStatusToManagerTimer(INodeConfiguration nodeConfiguration,
		                                   Uri callbackTemplateUri,
		                                   double interval = 10000) : base(interval)
		{
			// Validate arguments.
			nodeConfiguration.ThrowArgumentNullException();
			callbackTemplateUri.ThrowArgumentNullExceptionWhenNull();

			// Assign values.
			CancellationTokenSource = new CancellationTokenSource();

			NodeConfiguration = nodeConfiguration;

			WhoAmI = NodeConfiguration.CreateWhoIAm(Environment.MachineName);

			CallbackTemplateUri = callbackTemplateUri;

			Elapsed += OnTimedEvent;

			AutoReset = true;
		}

		private CancellationTokenSource CancellationTokenSource { get; set; }

		public string WhoAmI { get; private set; }

		public JobToDo JobToDo { get; set; }

		public INodeConfiguration NodeConfiguration { get; private set; }

		public Uri CallbackTemplateUri { get; set; }

		public event EventHandler TrySendStatusSucceded;

		public void InvokeTriggerTrySendStatusSucceded()
		{
			if (TrySendStatusSucceded != null)
			{
				TrySendStatusSucceded(this,
				                      EventArgs.Empty);
			}
		}

		protected virtual async Task<HttpResponseMessage> TrySendStatus(JobToDo jobToDo,
		                                                                CancellationToken cancellationToken)
		{
			try
			{
				var httpResponseMessage =
					await jobToDo.PostAsync(CallbackTemplateUri,
					                        cancellationToken);

				return httpResponseMessage;
			}

			catch (Exception exp)
			{
				LogHelper.LogErrorWithLineNumber(Logger,
				                                 "Error in TrySendStatus.",
				                                 exp);
				throw;
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (CancellationTokenSource != null &&
			    !CancellationTokenSource.IsCancellationRequested)
			{
				CancellationTokenSource.Cancel();
			}
		}

		private async void OnTimedEvent(object sender,
		                                ElapsedEventArgs e)
		{
			if (JobToDo == null)
			{
				return;
			}

			Stop();

			try
			{
				var httpResponseMessage = await TrySendStatus(new JobToDo
				{
					Id = JobToDo.Id
				}, CancellationTokenSource.Token);

				if (httpResponseMessage.IsSuccessStatusCode)
				{
					var msg = string.Format("{0} : Sent job status to manager ({3}) for job ( jobId, jobName ) : ( {1}, {2} )",
					                        WhoAmI,
					                        JobToDo.Id,
					                        JobToDo.Name,
					                        httpResponseMessage.RequestMessage.RequestUri);

					LogHelper.LogDebugWithLineNumber(Logger,
					                                 msg);


					InvokeTriggerTrySendStatusSucceded();
				}
				else
				{
					var msg =
						string.Format("{0} : Send status to manager failed for job ( jobId, jobName ) : ( {1}, {2} ). Reason : {3}",
						              WhoAmI,
						              JobToDo.Id,
						              JobToDo.Name,
						              httpResponseMessage.ReasonPhrase);

					LogHelper.LogInfoWithLineNumber(Logger,
					                                msg);
				}
			}

			catch (Exception exp)
			{
				var msg =
					string.Format("{0} : Send status to manager failed for job ( jobId, jobName ) : ( {1}, {2} ). Reason : {3}",
					              WhoAmI,
					              JobToDo.Id,
					              JobToDo.Name,
					              exp.Message);

				LogHelper.LogErrorWithLineNumber(Logger,
				                                 msg);
			}

			finally
			{
				Stop();
			}
		}
	}
}