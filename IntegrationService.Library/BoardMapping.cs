﻿
//------------------------------------------------------------------------------
// <copyright company="LeanKit Inc.">
//     Copyright (c) LeanKit Inc.  All rights reserved.
// </copyright> 
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kanban.API.Client.Library.TransferObjects;

namespace IntegrationService
{
	public class BoardMapping
	{

		public BoardMapping()
		{
			Types=new List<WorkItemType>();
			LaneToStatesMap = new Dictionary<long, List<string>>();
		}

		// populated by user, via config file
		public Identity Identity { get; set; }
		public List<string> QueryStates { get; set; }
		public Dictionary<long, List<string>> LaneToStatesMap { get; set; }
		public List<WorkItemType> Types { get; set; }
		public string Excludes { get; set; }
		public string Query { get; set; }
		public string IterationPath { get; set; }
		public bool CreateCards { get; set; }
		public bool UpdateCards { get; set; }
		public bool UpdateCardLanes { get; set; }
		public bool UpdateTargetItems { get; set; }
		public bool CreateTargetItems { get; set; }
		public bool TagCardsWithTargetSystemName { get; set; }

		// populated by app
		public string ExcludedTypeQuery { get; set; }
		public IList<Lane> ValidLanes { get; set; } 
		public IList<CardType> ValidCardTypes { get; set; }
		public long ArchiveLaneId { get; set; }

		public long LaneFromState(string state)
		{
			long laneId = LaneToStatesMap
				.Where(x => x.Value.Contains(state))
				.Select(x => x.Key)
				.FirstOrDefault();
			if (laneId == 0)
				laneId = ValidLanes.Select(x => x.Id).First();
			return laneId;
		}

		public override string ToString() 
		{
			var sb = new StringBuilder();
			sb.Append("     Identity :       " + Environment.NewLine + Identity);           
			if(LaneToStatesMap.Any())
			{
				sb.Append(Environment.NewLine);
				sb.Append("      Lane to States:   " + Environment.NewLine);
				foreach (var item in LaneToStatesMap)
				{
					sb.Append("        " + item.Key.ToString() + ": ");
					foreach(var state in item.Value)
						sb.Append(state + ", ");
					sb.Append(Environment.NewLine);
				}
			}

			sb.Append(Environment.NewLine);
			sb.Append("     Types :          " + Environment.NewLine);
			foreach (var workItemType in Types)
			{
				sb.AppendLine("          WorkItemType : " + workItemType);
			}
			sb.Append("     Excludes:        " + Excludes + Environment.NewLine);

			if (ValidLanes != null)
			{
				sb.Append(Environment.NewLine);
				sb.Append("     ValidLanes :     ");
				foreach (var validLane in ValidLanes)
				{
					sb.Append(validLane.Id + ", ");
				}
			}

			if (ValidCardTypes != null)
			{
				sb.Append(Environment.NewLine);
				sb.Append("     ValidCardTypes : ");
				foreach (var validCardType in ValidCardTypes)
				{
					sb.Append(validCardType.Name + ", ");
				}
			}

			sb.Append(Environment.NewLine);
			sb.AppendLine("     Query :                         " + Query);
			sb.AppendLine("     ArchiveLaneId :                 " + ArchiveLaneId.ToString());
			sb.AppendLine("     TagCardsWithTargetSystemName :  " + TagCardsWithTargetSystemName.ToString());
			sb.AppendLine("     CreateCards :                   " + CreateCards);
			sb.AppendLine("     CreateTargetItems :             " + CreateTargetItems);
			sb.AppendLine("     UpdateCards :                   " + UpdateCards);
			sb.AppendLine("     UpdateCardLanes :               " + UpdateCardLanes);
			sb.AppendLine("     UpdateTargetItems :             " + UpdateTargetItems);
			return sb.ToString();
		}
	}
}