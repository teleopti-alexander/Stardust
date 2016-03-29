﻿using System;
using Manager.Integration.Test.Constants;
using Manager.Integration.Test.Properties;

namespace Manager.Integration.Test.Helpers
{
	public class IntergrationControllerUriBuilder
	{
		private readonly UriBuilder _uriBuilder;

		private readonly UriBuilder _uriTemplateBuilder;

		public IntergrationControllerUriBuilder()
		{
			var baseAddress =
				new Uri(Settings.Default.IntegrationControllerBaseAddress);

			_uriBuilder = new UriBuilder(baseAddress);
			_uriTemplateBuilder = new UriBuilder(baseAddress);
		}

		public Uri GetAllManagersUri()
		{
			return CreateUri(IntegrationControllerRouteConstants.Managers);
		}


		public Uri GetManagerUriByManagerName(string managerName)
		{
			var uri =
				CreateUri(IntegrationControllerRouteConstants.ManagerById.Replace("{id}", managerName));

			return uri;
		}

		public Uri GetNodeUriByNodeName(string nodeName)
		{
			var uri=
				CreateUri(IntegrationControllerRouteConstants.NodeById.Replace("{id}",nodeName));

			return uri;
		}

		public Uri GetAllNodesUri()
		{
			return CreateUri(IntegrationControllerRouteConstants.Nodes);
		}

		public Uri CreateUri(string path)
		{
			_uriBuilder.Path = _uriTemplateBuilder.Path;

			_uriBuilder.Path += path;

			return _uriBuilder.Uri;
		}
	}
}