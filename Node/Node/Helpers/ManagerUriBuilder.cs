﻿using System;
using Stardust.Node.Constants;

namespace Stardust.Node.Helpers
{
    public class ManagerUriBuilder
    {
        private UriBuilder UriBuilder { get; set; }

        private Uri LocationUri { get; set; }


        public ManagerUriBuilder(Uri locationUri) : this(locationUri.ToString())
        {
        }

        public ManagerUriBuilder(string locationUri)
        {
            LocationUri = new Uri(locationUri);

            UriBuilder = new UriBuilder
            {
                Host = LocationUri.Host,
                Port = LocationUri.Port,
                Scheme = LocationUri.Scheme
            };
        }

        public Uri GetLocationUri()
        {
            return UriBuilder.Uri;
        }

        public Uri GetJobProgressUri()
        {
            return CreateUri(ManagerRouteConstants.JobProgress);
        }

        public Uri GetHeartbeatUri()
        {
            return CreateUri(ManagerRouteConstants.Heartbeat);
        }

        public Uri GetNodeHasBeenInitializedUri()
        {
            return CreateUri(ManagerRouteConstants.NodeHasBeenInitialized);
        }

        public Uri GetJobHasFailedUri()
        {
            return CreateUri(ManagerRouteConstants.JobFailed);
        }

        public Uri GetJobHasFailedUri(Guid guid)
        {
            string path = ManagerRouteConstants.JobFailed.Replace(ManagerRouteConstants.JobIdOptionalParameter,
                                                                  guid.ToString());

            return CreateUri(path);
        }

        public Uri GetJobHasBeenCanceledUri()
        {
            return CreateUri(ManagerRouteConstants.JobHasBeenCanceled);
        }

        public Uri GetJobHasBeenCanceledUri(Guid guid)
        {
            string path = ManagerRouteConstants.JobHasBeenCanceled.Replace(ManagerRouteConstants.JobIdOptionalParameter,
                                                                           guid.ToString());

            return CreateUri(path);
        }

        public Uri GetJobDoneUri()
        {
            return CreateUri(ManagerRouteConstants.JobDone);
        }

        public Uri GetJobDoneUri(Guid guid)
        {
            string path = ManagerRouteConstants.JobDone.Replace(ManagerRouteConstants.JobIdOptionalParameter,
                                                                guid.ToString());

            return CreateUri(path);
        }

        public Uri CreateUri(string path)
        {
            UriBuilder.Path = path;

            return UriBuilder.Uri;
        }
    }
}