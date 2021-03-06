﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Fiddler;
using Manager.Integration.Test.WPF.Annotations;

namespace Manager.Integration.Test.WPF.HttpListeners.Fiddler
{
	public class FiddlerCapture : IDisposable, INotifyPropertyChanged
	{
		private bool _isStarted;

		public EventHandler<FiddlerCaptureInformation> NewDataCapturedEventHandler;

		public FiddlerCapture(FiddlerCaptureUrlConfiguration fiddlerCaptureUrlConfiguration)
		{
			if (fiddlerCaptureUrlConfiguration == null)
			{
				throw new ArgumentNullException("fiddlerCaptureUrlConfiguration");
			}

			FiddlerCaptureUrlConfiguration = fiddlerCaptureUrlConfiguration;
		}

		public FiddlerCaptureUrlConfiguration FiddlerCaptureUrlConfiguration { get; private set; }

		public bool IsStarted
		{
			get { return _isStarted; }
			set
			{
				_isStarted = value;

				OnPropertyChanged();
			}
		}

		public void Dispose()
		{
			Stop();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void InvokeNewDataCapturedEventHandler(FiddlerCaptureInformation fiddlerCaptureInformation)
		{
			if (NewDataCapturedEventHandler != null)
			{
				NewDataCapturedEventHandler(this, fiddlerCaptureInformation);
			}
		}

		public void Start()
		{
			Task.Factory.StartNew(() =>
			{
				if (FiddlerApplication.IsStarted())
				{
					return;
				}

				FiddlerApplication.AfterSessionComplete += FiddlerApplicationOnAfterSessionComplete;

				FiddlerApplication.Startup(iListenPort: 8888,
				                           bRegisterAsSystemProxy: false,
				                           bDecryptSSL: true,
				                           bAllowRemote: true);

				IsStarted = true;
			});
		}

		public void Stop()
		{
			Task.Factory.StartNew(() =>
			{
				FiddlerApplication.AfterSessionComplete -= FiddlerApplicationOnAfterSessionComplete;

				if (!FiddlerApplication.IsStarted())
				{
					return;
				}

				FiddlerApplication.Shutdown();

				IsStarted = false;
			});
		}

		private void FiddlerApplicationOnAfterSessionComplete(Session sess)
		{
			// Ignore HTTPS connect requests
			if (sess.RequestMethod == "CONNECT")
			{
				return;
			}

			InvokeNewDataCapturedEventHandler(new FiddlerCaptureInformation
			{
				Uri = sess.fullUrl,
				ResponseCode = sess.responseCode,
				RequestHeaders = sess.oRequest.headers.ToString(),
				RequestBody = Encoding.UTF8.GetString(sess.RequestBody),
				RequestMethod = sess.RequestMethod
			});
		}

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}