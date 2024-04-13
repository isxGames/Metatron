using System;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public interface IEventCommunications
    {
        ConfigurationFilesUpdatedEvent ConfigurationFilesUpdatedEvent { get; }
        FleetAcceptInvitationEvent FleetAcceptInvitationEvent { get; }
        FleetNotificationEvent FleetNeedMemberSkillsEvent { get; }
        FleetMemberSkillsReceivedEvent FleetMemberSkillsReceivedEvent { get; }
        FleetNeedCombatAssistEvent FleetNeedCombatAssistEvent { get; }
        FleetNeedPickupEvent FleetNeedPickupEvent { get; }
        FleetNeedPickupEvent FleetNeedPickupConfirmedEvent { get; }
        FleetNeedPickupEvent FleetPickupCompletedEvent { get; }
        FleetNotificationEvent FleetSendAllPickupRequestsEvent { get; }
        AnomalyClaimedEvent AnomalyClaimedEvent { get; }
		SalvageBookmarkReadyEvent SalvageBookmarkReadyEvent { get; }
    }

    internal sealed class EventCommunications : ModuleBase, IDisposable, IEventCommunications
    {
		public static readonly string RelayGroup = "SB_Sessions";

        public ConfigurationFilesUpdatedEvent ConfigurationFilesUpdatedEvent { get; private set; }
        public FleetAcceptInvitationEvent FleetAcceptInvitationEvent { get; private set; }
        public FleetNotificationEvent FleetNeedMemberSkillsEvent { get; private set; }
        public FleetMemberSkillsReceivedEvent FleetMemberSkillsReceivedEvent { get; private set; }
        public FleetNeedCombatAssistEvent FleetNeedCombatAssistEvent { get; private set; }

        public FleetNeedPickupEvent FleetNeedPickupEvent { get; private set; }
        public FleetNeedPickupEvent FleetNeedPickupConfirmedEvent { get; private set; }
        public FleetNeedPickupEvent FleetPickupCompletedEvent { get; private set; }
        public FleetNotificationEvent FleetSendAllPickupRequestsEvent { get; private set; }

        public AnomalyClaimedEvent AnomalyClaimedEvent { get; private set; }

		public SalvageBookmarkReadyEvent SalvageBookmarkReadyEvent { get; private set; }

		private bool _isDisposed;

		public EventCommunications(ILogging logging) : base(logging)
		{
		    PulseFrequency = 5;
			ModuleManager.ModulesToDispose.Add(this);
			IsEnabled = false;
			ModuleName = "EventCommunications";

            ConfigurationFilesUpdatedEvent = new ConfigurationFilesUpdatedEvent(_logging, RelayGroup, MetatronEvents.ConfigurationFilesUpdated.ToString());
            FleetAcceptInvitationEvent = new FleetAcceptInvitationEvent(_logging, RelayGroup, MetatronEvents.FleetAcceptInvitation.ToString());
            FleetNeedMemberSkillsEvent = new FleetNotificationEvent(_logging, RelayGroup, MetatronEvents.FleetNeedMemberSkills.ToString());
            FleetMemberSkillsReceivedEvent = new FleetMemberSkillsReceivedEvent(_logging, RelayGroup, MetatronEvents.FleetMemberSkillsReceived.ToString());
            FleetNeedCombatAssistEvent = new FleetNeedCombatAssistEvent(_logging, RelayGroup, MetatronEvents.FleetNeedCombatAssist.ToString());
            FleetNeedPickupEvent = new FleetNeedPickupEvent(_logging, RelayGroup, MetatronEvents.FleetNeedPickup.ToString());
            FleetNeedPickupConfirmedEvent = new FleetNeedPickupEvent(_logging, RelayGroup, MetatronEvents.FleetNeedPickupConfirmed.ToString());
            FleetPickupCompletedEvent = new FleetNeedPickupEvent(_logging, RelayGroup, MetatronEvents.FleetPickupCompleted.ToString());
            FleetSendAllPickupRequestsEvent = new FleetNotificationEvent(_logging, RelayGroup, MetatronEvents.FleetSendAllPickupRequests.ToString());

            AnomalyClaimedEvent = new AnomalyClaimedEvent(_logging, RelayGroup, MetatronEvents.AnomalyClaimed.ToString());

			SalvageBookmarkReadyEvent = new SalvageBookmarkReadyEvent(_logging, RelayGroup, MetatronEvents.SalvageBookmarkReady.ToString());

			JoinRelayGroup();
		}

	    private static void JoinRelayGroup()
	    {
            //Join the Metatron relay group
	        LavishScript.ExecuteCommand(String.Format("Uplink RelayGroup -join {0}", RelayGroup));
	    }

	    public override void Pulse()
        {
            if (!ShouldPulse()) return;

            JoinRelayGroup();
        }

		private void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			_isDisposed = true;

			if (disposing)
			{
			    ConfigurationFilesUpdatedEvent.Dispose();
			    FleetAcceptInvitationEvent.Dispose();
			    FleetNeedMemberSkillsEvent.Dispose();
			    FleetMemberSkillsReceivedEvent.Dispose();
			    FleetNeedCombatAssistEvent.Dispose();
			    FleetNeedPickupEvent.Dispose();
			    FleetNeedPickupConfirmedEvent.Dispose();
			    FleetPickupCompletedEvent.Dispose();
			    FleetSendAllPickupRequestsEvent.Dispose();
                AnomalyClaimedEvent.Dispose();
				SalvageBookmarkReadyEvent.Dispose();
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}

	/// <summary>
	/// The various events Metatron can send and receive.
	/// </summary>
	public enum MetatronEvents
	{
		/// <summary>
		/// Signal to the fleet that we need a hauler to pickup a can.
		/// </summary>
		FleetNeedPickup,
		/// <summary>
		/// Signal to the fleet that we picked up a requested can.
		/// </summary>
		FleetNeedPickupConfirmed,
		/// <summary>
		/// Signal to the fleet that all requested pickups need re-sent.
		/// </summary>
		FleetSendAllPickupRequests,
		/// <summary>
		/// Signal to the fleet that the specified pickup request is complete.
		/// </summary>
		FleetPickupCompleted,
		/// <summary>
		/// Signal to the fleet that we need assistance with a target.
		/// </summary>
		FleetNeedCombatAssist,
		/// <summary>
		/// Signal to the listeners that we need their fleet-related skills.
		/// </summary>
		FleetNeedMemberSkills,
		/// <summary>
		/// Signal to the listeners the levels of our fleet-related skills.
		/// </summary>
		FleetMemberSkillsReceived,
		/// <summary>
		/// Signal to the listeners that they should accept a fleet invite from the sender.
		/// </summary>
		FleetAcceptInvitation,
		/// <summary>
		/// Signal to listeners that they need to update a configuration file
		/// </summary>
		ConfigurationFilesUpdated,
        /// <summary>
        /// Signal to listeners that an anomaly was claimed as of a given time.
        /// </summary>
        AnomalyClaimed,
        /// <summary>
		/// Signal to listeners that salvage bookmark is safe for salvagers
		/// </summary>
        SalvageBookmarkReady
    }
}
