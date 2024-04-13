using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LavishScriptAPI;

namespace Metatron.Core
{
	internal class EventCommunications : ModuleBase, IDisposable
	{
		public static readonly string RelayGroup = "SB_Sessions";
		public event EventHandler<Fleet_NeedPickupEventArgs> OnSB_Event_Fleet_NeedPickup;
		public event EventHandler<Fleet_NeedAssistEventArgs> OnSB_Event_Fleet_NeedAssist;
		public event EventHandler<Fleet_NeedTankEventArgs> OnSB_Event_Fleet_NeedTank;
		public event EventHandler<Fleet_NotificationEventArgs> OnSB_Event_Fleet_TankReady;
		public event EventHandler<Fleet_NotificationEventArgs> OnSB_Event_Fleet_TankNotReady;
		public event EventHandler<Info_UpdateFleetSkillsEventArgs> OnSB_Event_Info_UpdateFleetSkills;
		public event EventHandler<Fleet_NotificationEventArgs> OnSB_Event_Info_NeedFleetSkills;
		public event EventHandler<Notify_AcceptFleetInviteEventArgs> OnSB_Event_Notify_AcceptFleetInvite;
		public event EventHandler<Fleet_NeedPickupEventArgs> OnSB_Event_Fleet_DidPickup;
		public event EventHandler<Fleet_NeedPickupEventArgs> OnSB_Event_Fleet_AcknowledgePickupRequest;
		public event EventHandler<Notify_UpdateConfigurationFileEventArgs> OnSB_Event_Notify_UpdateConfigurationFile;
		public event EventHandler<Fleet_NotificationEventArgs> OnSB_Event_Fleet_SendPickupRequests;
		public event EventHandler<Fleet_NeedPickupEventArgs> OnSB_Event_Fleet_PickupRequestComplete;

		EventHandler<Fleet_NotificationEventArgs> SB_Event_Fleet_SendPickupRequestsEventHandler;
		List<Fleet_NeedPickupEventArgs> _sentPickupRequests = new List<Fleet_NeedPickupEventArgs>();

		EventHandler<Fleet_NeedPickupEventArgs> SB_Event_Fleet_PickupRequestCompleteEventHandler;

		private bool _isDisposed;

		public EventCommunications()
		{
			ModuleManager.ModulesToDispose.Add(this);
			IsEnabled = false;
			ObjectName = "EventCommunications";

			SB_Event_Fleet_SendPickupRequestsEventHandler = new EventHandler<Fleet_NotificationEventArgs>(HandleSendPickupRequests);
			SB_Event_Fleet_PickupRequestCompleteEventHandler = new EventHandler<Fleet_NeedPickupEventArgs>(HandlePickupRequestComplete);

			#region Register events
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Fleet_NeedAssist.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Fleet_NeedPickup.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Fleet_WillPickup.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Notify_PickupRequestAcknowledged.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Fleet_NeedTank.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Fleet_TankReady.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Fleet_TankNotReady.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Info_NeedFleetSkills.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Info_UpdateFleetSkills.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Notify_AcceptFleetInvite.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Notify_UpdateConfigurationFile.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Fleet_SendPickupRequests.ToString());
			LavishScript.Events.RegisterEvent(SB_Events.SB_Event_Fleet_PickupRequestComplete.ToString());
			#endregion

			#region Attach event targets
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Fleet_NeedAssist.ToString(),
				Handle_SB_Event_Fleet_NeedAssist);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Fleet_NeedPickup.ToString(),
				Handle_SB_Event_Fleet_NeedPickup);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Fleet_WillPickup.ToString(),
				Handle_SB_Event_Fleet_DidPickup);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Notify_PickupRequestAcknowledged.ToString(),
				Handle_SB_Event_Notify_AcknowledgePickupRequest);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Fleet_NeedTank.ToString(),
				Handle_SB_Event_Fleet_NeedTank);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Fleet_TankReady.ToString(),
				Handle_SB_Event_Fleet_TankReady);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Fleet_TankNotReady.ToString(),
				Handle_SB_Event_Fleet_TankNotReady);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Info_NeedFleetSkills.ToString(),
				Handle_SB_Event_Info_NeedFleetSkills);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Info_UpdateFleetSkills.ToString(),
				Handle_SB_Event_Info_UpdateFleetSkills);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Notify_AcceptFleetInvite.ToString(),
				Handle_SB_Event_Notify_AcceptFleetInvite);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Notify_UpdateConfigurationFile.ToString(),
				Handle_SB_Event_Notify_UpdateConfigurationFile);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Fleet_SendPickupRequests.ToString(),
				Handle_SB_Event_Fleet_SendPickupRequests);
			LavishScript.Events.AttachEventTarget(SB_Events.SB_Event_Fleet_PickupRequestComplete.ToString(),
				Handle_SB_Event_Fleet_PickupRequestComplete);
			#endregion

			//Join the Metatron relay group
			LavishScript.ExecuteCommand(String.Format("Uplink RelayGroup -join {0}", RelayGroup));
			//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
			//    "Ctor", String.Format("Joined RelayGroup {0}", RelayGroup)));
		}

		private void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			if (disposing)
			{
				#region Detach event targets
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Fleet_NeedAssist.ToString(),
					Handle_SB_Event_Fleet_NeedAssist);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Fleet_NeedPickup.ToString(),
					Handle_SB_Event_Fleet_NeedPickup);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Fleet_WillPickup.ToString(),
					Handle_SB_Event_Fleet_DidPickup);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Notify_PickupRequestAcknowledged.ToString(),
					Handle_SB_Event_Notify_AcknowledgePickupRequest);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Fleet_NeedTank.ToString(),
					Handle_SB_Event_Fleet_NeedTank);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Fleet_TankReady.ToString(),
					Handle_SB_Event_Fleet_TankReady);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Fleet_TankNotReady.ToString(),
					Handle_SB_Event_Fleet_TankNotReady);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Info_NeedFleetSkills.ToString(),
					Handle_SB_Event_Info_NeedFleetSkills);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Info_UpdateFleetSkills.ToString(),
					Handle_SB_Event_Info_UpdateFleetSkills);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Notify_AcceptFleetInvite.ToString(),
					Handle_SB_Event_Notify_AcceptFleetInvite);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Notify_UpdateConfigurationFile.ToString(),
					Handle_SB_Event_Notify_UpdateConfigurationFile);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Fleet_SendPickupRequests.ToString(),
					Handle_SB_Event_Fleet_SendPickupRequests);
				LavishScript.Events.DetachEventTarget(SB_Events.SB_Event_Fleet_PickupRequestComplete.ToString(),
					Handle_SB_Event_Fleet_PickupRequestComplete);
				#endregion
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		#region Re-send/acknowledge methods
		/// <summary>
		/// Re-execute the EventStrings for all sent pickup requests
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void HandleSendPickupRequests(object sender, Fleet_NotificationEventArgs e)
		{
			lock (this)
			{
				foreach (var request in _sentPickupRequests)
				{
					Send_SB_Event_Fleet_NeedPickup(request.SendingFleetMemberID, request.SolarSystemID, request.TargetCanEntityID, request.SendingFleetMemberEntityID,
						request.SendingFleetMemberName);
				}
			}
		}

		/// <summary>
		/// If the we have a pickup request matching the received complete one, remove it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void HandlePickupRequestComplete(object sender, Fleet_NeedPickupEventArgs e)
		{
			lock (this)
			{
				for (var index = 0; index < _sentPickupRequests.Count; index++)
				{
					var request = _sentPickupRequests[index];

					if (request.SendingFleetMemberID == e.SendingFleetMemberID &&
						request.TargetCanEntityID == e.TargetCanEntityID)
					{
						_sentPickupRequests.RemoveAt(index);
						index--;
						break;
					}
				}
			}
		}
		#endregion

		#region Event Handling Methods
		private void Handle_SB_Event_Fleet_NeedAssist(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Fleet_NeedAssist";
			LogTrace(methodName);

			var fe = new Fleet_NeedAssistEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received NeedAssist from {0}.", fe.SendingFleetMemberID);
			if (OnSB_Event_Fleet_NeedAssist != null)
				OnSB_Event_Fleet_NeedAssist(sender, fe);
		}

		private void Handle_SB_Event_Fleet_NeedPickup(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Fleet_NeedPickup";
			LogTrace(methodName);

			var fe = new Fleet_NeedPickupEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received NeedPickup from {0}.", fe.SendingFleetMemberID);
			if (OnSB_Event_Fleet_NeedPickup != null)
				OnSB_Event_Fleet_NeedPickup(this, fe);
		}

		private void Handle_SB_Event_Fleet_DidPickup(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Fleet_DidPickup";
			LogTrace(methodName);

			var fe = new Fleet_NeedPickupEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received DidPickup from {0}.", fe.SendingFleetMemberID);
			if (OnSB_Event_Fleet_DidPickup != null)
				OnSB_Event_Fleet_DidPickup(this, fe);
		}

		private void Handle_SB_Event_Notify_AcknowledgePickupRequest(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Notify_AcknowledgePickupRequest";
			LogTrace(methodName);

			var fe = new Fleet_NeedPickupEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received AckPickupRequest from {0}.", fe.SendingFleetMemberID);
			if (OnSB_Event_Fleet_AcknowledgePickupRequest != null)
				OnSB_Event_Fleet_AcknowledgePickupRequest(this, fe);
		}

		private void Handle_SB_Event_Fleet_NeedTank(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Fleet_NeedTank";
			LogTrace(methodName);

			var fe = new Fleet_NeedTankEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received NeedTank from {0}.", fe.SendingFleetMemberID);
			if (OnSB_Event_Fleet_NeedTank != null)
				OnSB_Event_Fleet_NeedTank(sender, fe);
		}

		private void Handle_SB_Event_Fleet_TankReady(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Fleet_TankReady";
			LogTrace(methodName);

			var fe = new Fleet_NotificationEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received TankReady from {0}.", fe.SendingFleetMemberID);
			if (OnSB_Event_Fleet_TankReady != null)
				OnSB_Event_Fleet_TankReady(sender, fe);
		}

		private void Handle_SB_Event_Fleet_TankNotReady(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Fleet_TankNotReady";
			LogTrace(methodName);

			var fe = new Fleet_NotificationEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received TankNotReady from {0}.", fe.SendingFleetMemberID);
			if (OnSB_Event_Fleet_TankNotReady != null)
				OnSB_Event_Fleet_TankNotReady(sender, fe);
		}

		private void Handle_SB_Event_Info_NeedFleetSkills(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Info_NeedFleetSkills";
			LogTrace(methodName);

			var eventArgs = new Fleet_NotificationEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received NeedFleetSkills from {0}.", eventArgs.SendingFleetMemberID);
			if (OnSB_Event_Info_NeedFleetSkills != null)
				OnSB_Event_Info_NeedFleetSkills(this, eventArgs);
		}

		private void Handle_SB_Event_Info_UpdateFleetSkills(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Info_UpdateFleetSkills";
			LogTrace(methodName);

			var eventArgs = new Info_UpdateFleetSkillsEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received UpdateFleetSkills from {0}.", eventArgs.SendingFleetMemberID);
			if (OnSB_Event_Info_UpdateFleetSkills != null)
				OnSB_Event_Info_UpdateFleetSkills(this, eventArgs);
		}

		private void Handle_SB_Event_Notify_AcceptFleetInvite(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Notify_AcceptFleetInvite";
			LogTrace(methodName);

			var eventArgs = new Notify_AcceptFleetInviteEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received AcceptFleetInvite from {0} ({1})",
				eventArgs.AcceptFrom, eventArgs.SendingFleetMemberID);
			if (OnSB_Event_Notify_AcceptFleetInvite != null)
				OnSB_Event_Notify_AcceptFleetInvite(this, eventArgs);
		}

		private void Handle_SB_Event_Notify_UpdateConfigurationFile(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Notify_UpdateConfigurationFile";
			LogTrace(methodName, "# Args: {0}", e.Args == null ? -1 : e.Args.Length);

			var eventArgs = new Notify_UpdateConfigurationFileEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received UpdateConfigurationFile[{0},{1},{2}]",
				eventArgs.UplinkName, eventArgs.OldFileName, eventArgs.NewFileName);
			//Make sure we're on our uplink
			if (Metatron.ModuleManager.UplinkName == eventArgs.UplinkName &&
				OnSB_Event_Notify_UpdateConfigurationFile != null)
				OnSB_Event_Notify_UpdateConfigurationFile(this, eventArgs);
		}

		private void Handle_SB_Event_Fleet_SendPickupRequests(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Fleet_SendPickupRequests";
			LogTrace(methodName);

			var eventArgs = new Fleet_NotificationEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received SendPickupRequests from {0}", eventArgs.SendingFleetMemberID);

			if (OnSB_Event_Fleet_SendPickupRequests != null)
				OnSB_Event_Fleet_SendPickupRequests(this, eventArgs);
		}

		private void Handle_SB_Event_Fleet_PickupRequestComplete(object sender, LSEventArgs e)
		{
			var methodName = "Handle_SB_Event_Fleet_PickupRequestComplete";
			LogTrace(methodName);

			var eventArgs = new Fleet_NeedPickupEventArgs(e);

			LogMessage(methodName, LogSeverityTypes.Debug, "Received PickupRequestComplete from {0}", eventArgs.SendingFleetMemberID);

			if (OnSB_Event_Fleet_PickupRequestComplete != null)
				OnSB_Event_Fleet_PickupRequestComplete(this, eventArgs);
		}
		#endregion

		#region Event Sending methods
		/// <summary>
		/// Signal to listeners that we need the fleet to assist with a given target.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		public void Send_SB_Event_Fleet_NeedAssist(Int64 sendingFleetMemberID, int solarSystemID)
		{
			var methodName = "Send_SB_Event_Fleet_NeedAssist";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3}]\"",
				RelayGroup, SB_Events.SB_Event_Fleet_NeedAssist, sendingFleetMemberID, solarSystemID);

			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal listeners that we need a fleet tank at a given location.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		/// <param name="isBookMark"></param>
		/// <param name="destination"></param>
		public void Send_SB_Event_Fleet_NeedTank(Int64 sendingFleetMemberID, int solarSystemID, bool isBookMark,
			object destination)
		{
			var methodName = "Send_Fleet_NeedTank";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3},{4},{5}]\"",
				RelayGroup, SB_Events.SB_Event_Fleet_NeedTank, sendingFleetMemberID, solarSystemID, isBookMark, destination);

			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal to listeners that we are a tank that is in position.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		public void Send_SB_Event_Fleet_TankReady(Int64 sendingFleetMemberID, int solarSystemID)
		{
			var methodName = "Send_Fleet_TankReady";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3}]\"",
				RelayGroup, SB_Events.SB_Event_Fleet_TankReady.ToString(), sendingFleetMemberID, solarSystemID);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal to listeners that we are no longer in position or actively tanking.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		public void Send_SB_Event_Fleet_TankNotReady(int sendingFleetMemberID, int solarSystemID)
		{
			var methodName = "Send_Fleet_TankNotReady";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3}]\"",
				RelayGroup, SB_Events.SB_Event_Fleet_TankNotReady.ToString(), sendingFleetMemberID.ToString(),
				solarSystemID.ToString());

			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal to listeners that we need a pickup.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		/// <param name="targetCanEntityID"></param>
		/// <param name="sendingFleetMemberEntityID"></param>
		public void Send_SB_Event_Fleet_NeedPickup(Int64 sendingFleetMemberID, int solarSystemID, Int64 targetCanEntityID, Int64 sendingFleetMemberEntityID, string sendingFleetMemberName)
		{
			var methodName = "Send_Fleet_NeedPickup";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3},{4},{5},{6}]\"",
				RelayGroup, SB_Events.SB_Event_Fleet_NeedPickup.ToString(), sendingFleetMemberID.ToString(),
				solarSystemID.ToString(), targetCanEntityID.ToString(), sendingFleetMemberEntityID.ToString(), sendingFleetMemberName);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);

			//Add a new PickupRequest to the list for re-sending if necessary
			var eventArgs = new Fleet_NeedPickupEventArgs(sendingFleetMemberID, solarSystemID, targetCanEntityID, sendingFleetMemberEntityID, sendingFleetMemberName);
			lock (this)
			{
				_sentPickupRequests.Add(eventArgs);
			}
		}

		/// <summary>
		/// Signal to listeners that a pickup request is complete.
		/// </summary>
		/// <param name="e"></param>
		public void Send_SB_Event_Fleet_PickupRequestComplete(Fleet_NeedPickupEventArgs e)
		{
			var methodName = "Send_Fleet_PickupRequesetComplete";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3},{4},{5},{6}]\"",
				RelayGroup, SB_Events.SB_Event_Fleet_PickupRequestComplete.ToString(), e.SendingFleetMemberID, e.SolarSystemID,
				e.TargetCanEntityID, e.SendingFleetMemberEntityID, e.SendingFleetMemberName);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal to listeners that we will pickup a specific can.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		/// <param name="targetCanEntityID"></param>
		/// <param name="sendingFleetMemberEntityID"></param>
		public void Send_SB_Event_Fleet_WillPickup(Int64 sendingFleetMemberID, int solarSystemID, Int64 targetCanEntityID, Int64 sendingFleetMemberEntityID, string sendingFleetMemberName)
		{
			var methodName = "Send_Fleet_WillPickup";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3},{4},{5},{6}]\"",
				RelayGroup, SB_Events.SB_Event_Fleet_WillPickup.ToString(), sendingFleetMemberID.ToString(),
				solarSystemID.ToString(), targetCanEntityID.ToString(), sendingFleetMemberEntityID.ToString(), sendingFleetMemberName);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal to listeners that we need their fleet skills.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		public void Send_SB_Event_Info_NeedFleetSkills(Int64 sendingFleetMemberID, int solarSystemID)
		{
			var methodName = "Send_Info_NeedFleetSkills";
			LogTrace(methodName);

			var eventString = String.Format("relay \"{0}\" \"Event[{1}]:Execute[{2},{3}]\"",
				RelayGroup, SB_Events.SB_Event_Info_NeedFleetSkills.ToString(),
				sendingFleetMemberID, solarSystemID);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal to listeners what our fleet skills are.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		/// <param name="inBoostShip"></param>
		/// <param name="leadership"></param>
		/// <param name="wingCommand"></param>
		/// <param name="fleetCommand"></param>
		/// <param name="miningDirector"></param>
		/// <param name="miningForeman"></param>
		/// <param name="armoredWarfare"></param>
		/// <param name="skirmishWarfare"></param>
		/// <param name="informationWarfare"></param>
		/// <param name="siegeWarfare"></param>
		/// <param name="warfareLinkSpecialist"></param>
		public void Send_SB_Event_Info_UpdateFleetSkills(Int64 sendingFleetMemberID, int solarSystemID, int inBoostShip, int leadership,
			int wingCommand, int fleetCommand, int miningDirector, int miningForeman, int armoredWarfare,
			int skirmishWarfare, int informationWarfare, int siegeWarfare, int warfareLinkSpecialist)
		{
			var methodName = "Send_UpdateFleetSkills";
			LogTrace(methodName);

			var eventString = String.Format("relay \"{0}\" \"Event[{1}]:Execute[{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}]\"",
				RelayGroup, SB_Events.SB_Event_Info_UpdateFleetSkills.ToString(),
				sendingFleetMemberID, solarSystemID, inBoostShip, leadership, wingCommand, fleetCommand, miningDirector,
				miningForeman, armoredWarfare, skirmishWarfare, informationWarfare, siegeWarfare, warfareLinkSpecialist);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal to listeners that they should accept a fleet invite from the sender.
		/// </summary>
		/// <param name="sendingFleetMemberID"></param>
		/// <param name="solarSystemID"></param>
		/// <param name="invitedBy"></param>
		public void Send_SB_Event_Notify_AcceptFleetInvite(Int64 sendingFleetMemberID, int solarSystemID, string invitedBy)
		{
			var methodName = "Send_Notify_AcceptFleetInvite";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3},{4}]\"",
				RelayGroup, SB_Events.SB_Event_Notify_AcceptFleetInvite.ToString(),
				sendingFleetMemberID, solarSystemID, invitedBy);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		/// <summary>
		/// Signal to listeners that they need to update a configuration file reference.
		/// </summary>
		/// <param name="oldFileName"></param>
		/// <param name="newFileName"></param>
		public void Send_SB_Event_Notify_UpdateConfigurationFile(string oldFileName, string newFileName)
		{
			var methodName = "Send_Notify_UpdateConfigurationFile";
			LogTrace(methodName);
			
			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3},{4}]\"",
				RelayGroup, SB_Events.SB_Event_Notify_UpdateConfigurationFile.ToString(),
				Metatron.ModuleManager.UplinkName, oldFileName, newFileName);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}

		public void Send_SB_Event_Fleet_SendPickupRequests(Int64 sendingFleetMemberID, int solarSystemID)
		{
			var methodName = "Send_SB_Event_Fleet_SendPickupRequests";
			LogTrace(methodName);

			var eventString = String.Format("relay \"other {0}\" \"Event[{1}]:Execute[{2},{3}]\"",
				RelayGroup, SB_Events.SB_Event_Fleet_SendPickupRequests.ToString(),
				sendingFleetMemberID, solarSystemID);
			LogMessage(methodName, LogSeverityTypes.Debug, "Sending {0}", eventString);
			LavishScript.ExecuteCommand(eventString);
		}
		#endregion
	}

	public class Fleet_NotificationEventArgs : LSEventArgs
	{
		public Int64 SendingFleetMemberID { get; private set; }
		public int SolarSystemID { get; private set; }
		public string ObjectName { get; set; }

		public Fleet_NotificationEventArgs() { }

		public Fleet_NotificationEventArgs(Int64 sendingFleetMemberID, int solarSystemID)
		{
			ObjectName = "NotificationEventArgs";
			SendingFleetMemberID = sendingFleetMemberID;
			SolarSystemID = solarSystemID;
		}

		//Call a function to parse the values of the args
		public Fleet_NotificationEventArgs(LSEventArgs copy)
		{
			ObjectName = "NotificationEventArgs";
			Initialize(copy);
		}

		private void Initialize(LSEventArgs copy)
		{
			var methodName = "Initialize";

		    Int64 sendingFleetMemberID = -1;
            var solarSystemID = -1;

			if (!Int64.TryParse(copy.Args[0], out sendingFleetMemberID))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse sending fleet member ID {0}",
					copy.Args[0]);
			}
			if (!int.TryParse(copy.Args[1], out solarSystemID))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse sending solar system ID {0}",
					copy.Args[1]);
			}
			SendingFleetMemberID = sendingFleetMemberID;
			SolarSystemID = solarSystemID;
		}
	}

	public sealed class Fleet_NeedPickupEventArgs : Fleet_NotificationEventArgs
	{
		public Int64 TargetCanEntityID { get; private set; }
		public Int64 SendingFleetMemberEntityID { get; private set; }
		public string SendingFleetMemberName { get; private set; }

		public Fleet_NeedPickupEventArgs(Int64 sendingFleetMemberID, int solarSystemID, Int64 targetCanEntityID, Int64 sendingFleetMemberEntityID, string sendingFleetMemberName) :
			base(sendingFleetMemberID, solarSystemID)
		{
			ObjectName = "NeedPickupEventArgs";
			TargetCanEntityID = targetCanEntityID;
			SendingFleetMemberEntityID = sendingFleetMemberEntityID;
			SendingFleetMemberName = sendingFleetMemberName;
		}

		public Fleet_NeedPickupEventArgs(LSEventArgs copy)
			: base(copy)
		{
			ObjectName = "NeedPickupEventArgs";
			Initialize(copy);
		}

		private void Initialize(LSEventArgs copy)
		{
			var methodName = "Initialize";
			Int64 targetCanEntityID, sendingFleetMemberEntityID;

			if (!Int64.TryParse(copy.Args[2], out targetCanEntityID))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target can entity ID {0}", copy.Args[2]);
			}
			if (!Int64.TryParse(copy.Args[3], out sendingFleetMemberEntityID))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse sending fleet member entity ID {0}", copy.Args[3]);
			}
			TargetCanEntityID = targetCanEntityID;
			SendingFleetMemberEntityID = sendingFleetMemberEntityID;
		}
	}

	public sealed class Fleet_NeedAssistEventArgs : Fleet_NotificationEventArgs
	{
		public Int64 TargetEntityID { get; private set; }

		public Fleet_NeedAssistEventArgs(Int64 sendingFleetMemberID, int solarSystemID, Int64 targetEntityID) :
			base(sendingFleetMemberID, solarSystemID)
		{
			ObjectName = "NeedAssistEventArgs";
			TargetEntityID = targetEntityID;
		}

		public Fleet_NeedAssistEventArgs(LSEventArgs copy)
			: base(copy)
		{
			ObjectName = "NeedAssistEventArgs";
			Initialize(copy);
		}

		private void Initialize(LSEventArgs copy)
		{
			var methodName = "Initialize";
			Int64 targetEntityID = -1;

			if (!Int64.TryParse(copy.Args[2], out targetEntityID))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse target entity ID {0}", copy.Args[2]);
			}
			TargetEntityID = targetEntityID;
		}
	}

	public sealed class Fleet_NeedTankEventArgs : Fleet_NotificationEventArgs
	{
		public bool DestinationIsBookMark { get; private set; }
		public object DestinationID { get; private set; }

		public Fleet_NeedTankEventArgs(Int64 sendingFleetMemberID, int solarSystemID, bool destinationIsBookMark, object destinationID) :
			base(sendingFleetMemberID, solarSystemID)
		{
			ObjectName = "NeedTankEventArg";
			DestinationIsBookMark = destinationIsBookMark;
			DestinationID = destinationID;
		}

		public Fleet_NeedTankEventArgs(LSEventArgs copy)
			: base(copy)
		{
			ObjectName = "NeedTankEventArgs";
			Initialize(copy);
		}

		private void Initialize(LSEventArgs copy)
		{
			var methodName = "Initialize";
			Int64 nearestEntityID = -1;
			bool isBookmark;
			var bookMarkLabel = string.Empty;

			if (!bool.TryParse(copy.Args[2], out isBookmark))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse \'is bookmark\' {0}", copy.Args[2]);
			}
			if (isBookmark)
			{
				bookMarkLabel = copy.Args[3];
			}
			else
			{
				if (!Int64.TryParse(copy.Args[3], out nearestEntityID))
				{
					Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse \'is bookmark\' {0}", copy.Args[2]);
				}
			}

			if (isBookmark)
			{
				DestinationIsBookMark = true;
				DestinationID = bookMarkLabel;
			}
			else
			{
				DestinationIsBookMark = false;
				DestinationID = nearestEntityID;
			}
		}
	}

	public sealed class Info_UpdateFleetSkillsEventArgs : Fleet_NotificationEventArgs
	{
		public int InBoostShip { get; private set; }
		public int Leadership { get; private set; }
		public int WingCommand { get; private set; }
		public int FleetCommand { get; private set; }
		public int MiningDirector { get; private set; }
		public int MiningForeman { get; private set; }
		public int ArmoredWarfare { get; private set; }
		public int SkirmishWarfare { get; private set; }
		public int InformationWarfare { get; private set; }
		public int SiegeWarfare { get; private set; }
		public int WarfareLinkSpecialist { get; private set; }

		public Info_UpdateFleetSkillsEventArgs(Int64 sendingFleetMemberID, int solarSystemID, int inBoostShip, int leadership,
			int wingCommand, int fleetCommand, int miningDirector, int miningForeman, int armoredWarfare,
			int skirmishWarfare, int informationWarfare, int siegeWarfare, int warfareLinkSpecialist) :
			base(sendingFleetMemberID, solarSystemID)
		{
			ObjectName = "UpdateFleetSkillsEventArgs";
			InBoostShip = inBoostShip;
			Leadership = leadership;
			WingCommand = wingCommand;
			FleetCommand = fleetCommand;
			MiningDirector = miningDirector;
			MiningForeman = miningForeman;
			ArmoredWarfare = armoredWarfare;
			SkirmishWarfare = skirmishWarfare;
			InformationWarfare = informationWarfare;
			SiegeWarfare = siegeWarfare;
			WarfareLinkSpecialist = warfareLinkSpecialist;
		}

		public Info_UpdateFleetSkillsEventArgs(LSEventArgs copy)
			: base(copy)
		{
			ObjectName = "UpdateFleetSkillsEventArgs";
			Initialize(copy);
		}

		private void Initialize(LSEventArgs copy)
		{
			var methodName = "Initialize";
			int inBoostShip, leadership, wingCommand, fleetCommand, miningDirector, miningForeman, armoredWarfare,
				skirmishWarfare, informationWarfare, siegeWarfare, warfareLinkSpecialist;

			if (!int.TryParse(copy.Args[2], out inBoostShip))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse in boost ship {0}", copy.Args[2]);
			}
			if (!int.TryParse(copy.Args[3], out leadership))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse leadership level {0}", copy.Args[2]);
			}
			if (!int.TryParse(copy.Args[4], out wingCommand))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse wing command level {0}", copy.Args[3]);
			}
			if (!int.TryParse(copy.Args[5], out fleetCommand))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse fleet command level {0}", copy.Args[4]);
			}
			if (!int.TryParse(copy.Args[6], out miningDirector))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse mining director level {0}", copy.Args[5]);
			}
			if (!int.TryParse(copy.Args[7], out miningForeman))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse mining foreman level {0}", copy.Args[6]);
			}
			if (!int.TryParse(copy.Args[8], out armoredWarfare))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse armor warfare level {0}", copy.Args[7]);
			}
			if (!int.TryParse(copy.Args[9], out skirmishWarfare))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse skirmish warfare level {0}", copy.Args[8]);
			}
			if (!int.TryParse(copy.Args[10], out informationWarfare))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse information warfare level {0}", copy.Args[9]);
			}
			if (!int.TryParse(copy.Args[11], out siegeWarfare))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse siege warfare level {0}", copy.Args[10]);
			}
			if (!int.TryParse(copy.Args[12], out warfareLinkSpecialist))
			{
				Metatron.Logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse warfare link specialist level {0}", copy.Args[11]);
			}

			InBoostShip = inBoostShip;
			Leadership = leadership;
			WingCommand = wingCommand;
			FleetCommand = fleetCommand;
			MiningDirector = miningDirector;
			MiningForeman = miningForeman;
			ArmoredWarfare = armoredWarfare;
			SkirmishWarfare = skirmishWarfare;
			InformationWarfare = informationWarfare;
			SiegeWarfare = siegeWarfare;
			WarfareLinkSpecialist = warfareLinkSpecialist;
		}
	}

	public sealed class Notify_AcceptFleetInviteEventArgs : Fleet_NotificationEventArgs
	{
		public string AcceptFrom { get; private set; }

		public Notify_AcceptFleetInviteEventArgs(long sendingFleetMemberID, int solarSystemID, string acceptFrom) :
			base(sendingFleetMemberID, solarSystemID)
		{
			ObjectName = "Notify_AcceptFleetInviteEventArgs";
			AcceptFrom = acceptFrom;
		}

		public Notify_AcceptFleetInviteEventArgs(LSEventArgs copy)
			: base(copy)
		{
			ObjectName = "Notify_AcceptFleetInviteEventArgs";
			Initialize(copy);
		}

		private void Initialize(LSEventArgs copy)
		{
			AcceptFrom = copy.Args[2];
		}
	}

	public sealed class Notify_UpdateConfigurationFileEventArgs : Fleet_NotificationEventArgs
	{
		public string UplinkName { get; private set; }
		public string OldFileName { get; private set; }
		public string NewFileName { get; private set; }

		public Notify_UpdateConfigurationFileEventArgs(LSEventArgs copy) : base()
		{
			ObjectName = "Notify_UpdateConfigurationFileEventArgs";
			Initialize(copy);
		}

		private void Initialize(LSEventArgs copy)
		{
			UplinkName = copy.Args[0];
			OldFileName = copy.Args[1];
			NewFileName = copy.Args[2];
		}
	}

	/// <summary>
	/// The various events Metatron can send and receive.
	/// </summary>
	public enum SB_Events
	{
		/// <summary>
		/// Signal to the fleet that we need a hauler to pickup a can.
		/// </summary>
		SB_Event_Fleet_NeedPickup,
		/// <summary>
		/// Signal to the fleet that we picked up a requested can.
		/// </summary>
		SB_Event_Fleet_WillPickup,
		/// <summary>
		/// Signal to the fleet that all requested pickups need re-sent.
		/// </summary>
		SB_Event_Fleet_SendPickupRequests,
		/// <summary>
		/// Signal to the fleet that the specified pickup request is complete.
		/// </summary>
		SB_Event_Fleet_PickupRequestComplete,
		/// <summary>
		/// Signal to the fleet that we need assistance with a target.
		/// </summary>
		SB_Event_Fleet_NeedAssist,
		/// <summary>
		/// Signal to the fleet that we need a tank at a specific location.
		/// </summary>
		SB_Event_Fleet_NeedTank,
		/// <summary>
		/// Signal to the fleet that a tank is in position at a specific location.
		/// </summary>
		SB_Event_Fleet_TankReady,
		/// <summary>
		/// Signal to the fleet that a tank that was in position is no longer in position.
		/// </summary>
		SB_Event_Fleet_TankNotReady,
		/// <summary>
		/// Signal to the listeners that we need their fleet-related skills.
		/// </summary>
		SB_Event_Info_NeedFleetSkills,
		/// <summary>
		/// Signal to the listeners the levels of our fleet-related skills.
		/// </summary>
		SB_Event_Info_UpdateFleetSkills,
		/// <summary>
		/// Signal to the listeners that they should accept a fleet invite from the sender.
		/// </summary>
		SB_Event_Notify_AcceptFleetInvite,
		/// <summary>
		/// Signal to listeners that we are going to try to pickup a specific can.
		/// </summary>
		SB_Event_Notify_PickupRequestAcknowledged,
		/// <summary>
		/// Signal to listeners that they need to update a configuration file
		/// </summary>
		SB_Event_Notify_UpdateConfigurationFile
	}
}
