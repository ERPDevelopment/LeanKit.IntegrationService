//------------------------------------------------------------------------------
// <copyright company="LeanKit Inc.">
//     Copyright (c) LeanKit Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using IntegrationService.Util;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace IntegrationService.Targets.TFS
{
	public class TfsConnection : IConnection
	{
		private Uri _projectCollectionUri;
		private ICredentials _projectCollectionNetworkCredentials;
		private TfsTeamProjectCollection _projectCollection;
		private WorkItemStore _projectCollectionWorkItemStore;
		private BasicAuthCredential _basicAuthCredential;
		private TfsClientCredentials _tfsClientCredentials;

		public ConnectionResult Connect(string protocol, string host, string user, string password)
		{
			string.Format("Connecting to TFS '{0}'", host).Debug();

			if (protocol.ToLowerInvariant().StartsWith("file"))
			{
				string.Format("TFS integration cannot use a file datasource '{0}'.", host).Error();
				return ConnectionResult.InvalidUrl;
			}

			try
			{
				_projectCollectionUri = new Uri(host);
			}
			catch (UriFormatException ex)
			{
				string.Format("Invalid project URL '{0}': {1}", host, ex.Message).Error();
				return ConnectionResult.InvalidUrl;
			}

			//This is used to query TFS for new WorkItems
			try
			{
				if (_projectCollectionNetworkCredentials == null)
				{
					_projectCollectionNetworkCredentials = new NetworkCredential(user, password);

					// if this is hosted TFS then we need to authenticate a little different
					// see this for setup to do on visualstudio.com site:
					// http://blogs.msdn.com/b/buckh/archive/2013/01/07/how-to-connect-to-tf-service-without-a-prompt-for-liveid-credentials.aspx
					if (_projectCollectionUri.Host.ToLowerInvariant().Contains(".visualstudio.com"))
					{

						if (_basicAuthCredential == null)
							_basicAuthCredential = new BasicAuthCredential(_projectCollectionNetworkCredentials);

						if (_tfsClientCredentials == null)
						{
							_tfsClientCredentials = new TfsClientCredentials(_basicAuthCredential);
							_tfsClientCredentials.AllowInteractive = false;
						}

					}
					if (_projectCollection == null)
					{
						_projectCollection = _tfsClientCredentials != null
							? new TfsTeamProjectCollection(_projectCollectionUri, _tfsClientCredentials)
							: new TfsTeamProjectCollection(_projectCollectionUri, _projectCollectionNetworkCredentials);
					}
					_projectCollectionWorkItemStore = new WorkItemStore(_projectCollection);

				}

				if (_projectCollectionWorkItemStore == null)
					_projectCollectionWorkItemStore = new WorkItemStore(_projectCollection);

			}
			catch (Exception e)
			{
				string.Format("Failed to connect: {0}", e.Message).Error(e);
				return ConnectionResult.FailedToConnect;
			}

			return ConnectionResult.Success;
		}

		public List<Project> GetProjects()
		{
			"Getting list of TFS projects".Debug();

			if (_projectCollection == null) return null;

			var iss = _projectCollection.GetService<ICommonStructureService>();
			var projects = iss.ListProjects();

			if (projects == null) return null;

			var projs = new List<Project>();
			var states = new SortedList<string, State>();
			foreach (var projectInfo in projects)
			{
				var workItems = GetWorkItemTypes(projectInfo);
				foreach (var workItem in workItems)
				{
					foreach (var state in workItem.States)
					{
						if (!states.ContainsKey(state.Name))
							states.Add(state.Name, state);
					}
				}

				var structure = iss.ListStructures(projectInfo.Uri);
				var nodeUri = structure.First(x => x.StructureType == StructureType.ProjectLifecycle).Uri;
				var xml = iss.GetNodesXml(new[] {nodeUri}, true);

				var iterationPaths = GetIterationPaths(xml.FirstChild);
				var editedIterationPaths = new List<string>();
				foreach (var item in iterationPaths)
				{
					var removeLeading = item.Substring(1);
					var noIteration = removeLeading.Remove(removeLeading.IndexOf("\\Iteration"), 10);
					editedIterationPaths.Add(noIteration);
				}

				projs.Add(new Project(projectInfo.Uri, projectInfo.Name, GetWorkItemTypes(projectInfo), states.Values.ToList(),
					editedIterationPaths));
			}
			return projs;
		}

		private List<Type> GetWorkItemTypes(ProjectInfo projectInfo)
		{
			string.Format("Getting TFS Work Item Types for project {0}", projectInfo.Name).Debug();
			var workItemTypes = new List<Type>();

			if (_projectCollectionWorkItemStore == null) return workItemTypes;

			var proj = _projectCollectionWorkItemStore.Projects[projectInfo.Name];

			if (proj == null || proj.WorkItemTypes == null) return workItemTypes;

			foreach (Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItemType workItemType in proj.WorkItemTypes)
			{
				workItemTypes.Add(new Type(workItemType.Name, GetStates(workItemType)));
			}
			return workItemTypes;
		}

		private List<State> GetStates(Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItemType workItemType)
		{
			string.Format("Getting TFS States for work item type '{0}'", workItemType.Name).Debug();
			var states = new SortedList<string, State>();

			var workItemTypeXml = workItemType.Export(false);
			var transitionsList = workItemTypeXml.GetElementsByTagName("TRANSITIONS");
			var transitions = transitionsList[0];

			foreach (XmlNode transition in transitions)
			{
				if (transition.Attributes == null) continue;

				if (transition.Attributes["to"] != null)
				{
					var toState = transition.Attributes["to"].Value;
					if (!string.IsNullOrEmpty(toState) && !states.ContainsKey(toState))
					{
						states.Add(toState, new State(toState));
					}
				}

				if (transition.Attributes["from"] != null)
				{
					var fromState = transition.Attributes["from"].Value;
					if (!string.IsNullOrEmpty(fromState) && !states.ContainsKey(fromState))
					{
						states.Add(fromState, new State(fromState));
					}
				}
			}

			return states.Values.ToList();
		}

		private List<string> GetIterationPaths(XmlNode node)
		{
			"Getting TFS iteration paths".Debug();

			var paths = new List<string>();

			if (node == null) return paths;

			if (node.Attributes != null && node.Attributes["Path"] != null)
				paths.Add(node.Attributes["Path"].Value);

			if (!node.HasChildNodes) return paths;

			foreach (XmlNode child in node.ChildNodes)
			{
				var childPaths = GetIterationPaths(child);
				if (childPaths != null && childPaths.Count > 0)
					paths.AddRange(childPaths);
			}
			return paths;
		}

	}
}
