using System;
using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE;
using LavishScriptAPI;
using Metatron.Core.EventCommunication;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    // ReSharper disable StringIndexOfIsCultureSpecific.1
    // ReSharper disable PossibleMultipleEnumeration
    internal sealed class Fleet : ModuleBase, IFleet
    {
		public int WingCommandLevel = -1, FleetCommandLevel = -1, MiningForemanLevel = -1,
		MiningDirectorLevel = -1, ArmoredWarfareLevel = -1, SkirmishWarfareLevel = -1, InformationWarfareLevel = -1,
		SiegeWarfareLevel = -1, WarfareLinkSpecialistLevel = -1, LeadershipLevel = -1;

        // ReSharper disable ConvertToConstant.Local
		private static readonly string RoleFleetCommander = "Fleet Cmdr";
		private static readonly string RoleWingCommander = "Wing Cmdr";
		private static readonly string RoleSquadCommander = "Squad Cmdr";
		//private static readonly string RoleMember = "Member";
        // ReSharper restore ConvertToConstant.Local

		private string _acceptFrom = string.Empty;

		private readonly EventHandler<BaseEventArgs> _fleetNeedMemberSkills;
		private readonly EventHandler<FleetAcceptInvitationEventArgs> _fleetAcceptInvitation;
		private readonly EventHandler<FleetMemberSkillsReceivedEventArgs> _fleetMemberSkillsReceived;

        private volatile List<BaseEventArgs> _infoRequestsToProcess = new List<BaseEventArgs>();
        private volatile List<FleetAcceptInvitationEventArgs> _acceptFleetInviteNotificationsToProcess = new List<FleetAcceptInvitationEventArgs>();

        private readonly Dictionary<Int64, FleetMemberSkillsReceivedEventArgs> _fleetInfoByCharId = new Dictionary<Int64, FleetMemberSkillsReceivedEventArgs>();
		private DateTime _lastSkillUpdate = DateTime.Now, _lastFleetAccept = DateTime.Now;
		private readonly Dictionary<Int64, DateTime> _nextInviteTimeByCharId = new Dictionary<Int64, DateTime>();
		private Int64 _fleetBuddyCharId = -1;

	    private readonly Random _random = new Random(DateTime.Now.Millisecond);

		public Fleet()
		{
			ModuleName = "Fleet";
			IsEnabled = true;

			//Set up the handling of the skills request event
            _fleetNeedMemberSkills = OnFleetNeedSkills;
			Metatron.EventCommunications.FleetNeedMemberSkillsEvent.EventRaised += _fleetNeedMemberSkills;
			//Set up handling of the "accept fleet request" event
            _fleetAcceptInvitation = OnFleetAcceptInvitation;
			Metatron.EventCommunications.FleetAcceptInvitationEvent.EventRaised += _fleetAcceptInvitation;
			//Set up handling of the update event
            _fleetMemberSkillsReceived = OnFleetMemberSkillsReceived;
			Metatron.EventCommunications.FleetMemberSkillsReceivedEvent.EventRaised += _fleetMemberSkillsReceived;

            ModuleManager.ModulesToPulse.Add(this);
			PulseFrequency = 3;
		}

		public override void Pulse()
		{
            var methodName = "Pulse";
			LogTrace(methodName);

            //Make sure I'm not undocking!
			if (!ShouldPulse())
				return;

			if (DateTime.Now.CompareTo(_lastSkillUpdate) >= 0)
			{
				_lastSkillUpdate = DateTime.Now.AddSeconds(1800);
				DetermineFleetSkills();
			}

			//Send off any needed updates
			HandleFleetInfoRequests();

		    if (Metatron.Config.FleetConfig.DoFleetInvites)
		    {
		        //If we have any buddies to invite, this is where we do it
				FormFleet();
		    }
            else
		        HandleFleetInviteAcceptRequests();
		}

	    private void FormFleet()
	    {
	        var methodName = "FormFleet";
	        foreach (var buddyCharId in Metatron.Config.FleetConfig.BuddyCharIDsToInvite)
	        {
	            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
	            //	"Pulse", String.Format("Checking buddy {0} for fleet invite", i)));
					
	            //Make sure the member isn't already a fleet meber
	            if (Metatron.MeCache.FleetMembers.Any(fleetMember => fleetMember.CharID == buddyCharId)) 
	                continue;

	            var buddyInvited = false;
// ReSharper disable AccessToForEachVariableInClosure
	            foreach (var being in Metatron.MeCache.Buddies.Where(being => being.CharID == buddyCharId))
// ReSharper restore AccessToForEachVariableInClosure
	            {
                    if (!being.IsOnline)
                    {
                        if (_nextInviteTimeByCharId.ContainsKey(being.CharID))
                            _nextInviteTimeByCharId.Remove(being.CharID);
                    }

	                //If we haven't seen the fleet member yet, add them and wait a bit to invite them.
	                if (!_nextInviteTimeByCharId.ContainsKey(being.CharID))
	                {
	                    _nextInviteTimeByCharId.Add(being.CharID, GetRandomInviteDelay());
	                    continue;
	                }

	                //If we can't yet invite the fleet member, continue
	                if (DateTime.Now.CompareTo(_nextInviteTimeByCharId[being.CharID]) < 0)
	                    continue;

	                LogMessage(methodName, LogSeverityTypes.Standard, "Inviting buddy {0} ({1}) to fleet.",
	                           being.Name, being.CharID);

	                //Tell the fleet to accept an invite from me
	                Metatron.EventCommunications.FleetAcceptInvitationEvent.SendEventFromArgs(
	                    Metatron.MeCache.CharId, Metatron.MeCache.SolarSystemId, Metatron.MeCache.Name);

	                //Also tell them to send me info
	                Metatron.EventCommunications.FleetNeedMemberSkillsEvent.SendEventFromArgs(
	                    Metatron.MeCache.CharId, Metatron.MeCache.SolarSystemId);

	                being.InviteToFleet();
	                buddyInvited = true;

	                //Nexxt invite attempt should be randomized too
	                _nextInviteTimeByCharId[being.CharID] = GetRandomInviteDelay();
	                break;
	            }

	            if (buddyInvited)
	            {
	                return;
	            }
	        }

	        //Move any fleet members
	        MoveFleetMembers();
	    }

	    private void HandleFleetInviteAcceptRequests()
	    {
	        var methodName = "HandleFleetInviteAcceptRequests";
	        foreach (var eventArgs in _acceptFleetInviteNotificationsToProcess)
	        {
// ReSharper disable AccessToForEachVariableInClosure
	            foreach (var being in Metatron.MeCache.Buddies.Where(being => eventArgs.SendingFleetMemberCharId == being.CharID))
// ReSharper restore AccessToForEachVariableInClosure
	            {
	                LogMessage(methodName, LogSeverityTypes.Debug, "We should accept a fleet invite from buddy {0} ({1})",
	                           eventArgs.AcceptFrom, eventArgs.SendingFleetMemberCharId);
	                _acceptFrom = eventArgs.AcceptFrom;
	                _fleetBuddyCharId = being.CharID;
	                break;
	            }
	        }
	        lock (this)
	        {
	            _acceptFleetInviteNotificationsToProcess.Clear();
	        }

	        if (DateTime.Now.CompareTo(_lastFleetAccept) >= 0)
	        {
	            var fleetMemberFound = Metatron.MeCache.FleetMembers.Any(fleetMember => fleetMember.CharID == _fleetBuddyCharId);

                if (Metatron.MeCache.Me.Fleet.ID >= 0 && _acceptFrom != string.Empty && !fleetMemberFound && _fleetBuddyCharId >= 0)
	            {
	                LogMessage(methodName, LogSeverityTypes.Standard, "Leaving current fleet in order to join fleet from buddy {0} ({1})",
	                           _acceptFrom, _fleetBuddyCharId);

	                //TODO: Need to check EVE.NextSessionChange == 0 before leaving fleet, once CyberTech adds it.
                    Metatron.MeCache.Me.Fleet.LeaveFleet();
	                _fleetBuddyCharId = -1;
	                return;
	            }
	        }

	        if (_acceptFrom == string.Empty || !Metatron.MeCache.IsFleetInvited) 
	            return;

            var invitationText = Metatron.MeCache.Me.Fleet.InvitationText;
	        var lengthOfName = invitationText.IndexOf(" wants you");
	        var invitedBy = invitationText.Substring(0, lengthOfName);
	        //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
	        //	"Pulse", String.Format("Parsed invitedBy of {0}", invitedBy)));
	        if (invitedBy != _acceptFrom) 
	            return;

	        LogMessage(methodName, LogSeverityTypes.Standard, "Accepting fleet invitation from {0}",
	                   invitedBy);
	        //TODO: Need to check EVE.NextSessionChange == 0 before joining fleet, once CyberTech adds it.
            Metatron.MeCache.Me.Fleet.AcceptInvite();
	        _lastFleetAccept = DateTime.Now.AddSeconds(30);
	        _acceptFrom = string.Empty;
	    }

	    private DateTime GetRandomInviteDelay()
	    {
	        //Get a random delay between 30 and 75 seconds
	        var delay = 30 + _random.Next(45);
	        return DateTime.Now.AddSeconds(delay);
	    }

	    private void HandleFleetInfoRequests()
	    {
	        if (_infoRequestsToProcess.Any())
	        {
	            var inBoostShip = 0;
	            if (Metatron.Ship.GangLinkModules.Any())
	            {
	                inBoostShip = 1;
	            }

	            Metatron.EventCommunications.FleetMemberSkillsReceivedEvent.SendEventFromArgs(
	                Metatron.MeCache.CharId, Metatron.MeCache.SolarSystemId, inBoostShip, LeadershipLevel,
	                WingCommandLevel, FleetCommandLevel, MiningDirectorLevel, MiningForemanLevel,
	                ArmoredWarfareLevel, SkirmishWarfareLevel, InformationWarfareLevel,
	                SiegeWarfareLevel, WarfareLinkSpecialistLevel);
	        }
			
	        lock (this)
	        {
	            _infoRequestsToProcess.Clear();
	        }
	    }

	    private void MoveFleetMembers()
		{
            var methodName = "MoveFleetMembers";
			LogTrace(methodName);

			var fleetMembersById = Metatron.MeCache.FleetMembersById;
            if (!fleetMembersById.ContainsKey(Metatron.MeCache.CharId))
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "I am not currently in fleet; not moving members.");
                return;
            }

            var fleetSize = Metatron.MeCache.FleetMembers.Count;
            List<FleetMemberSkillsReceivedEventArgs> fleetInfo;
            var wings = Metatron.MeCache.Me.Fleet.GetWings();
            List<long> squads;

			lock (this)
			{
				fleetInfo = (from fleetMemberSkills in _fleetInfoByCharId.Values
							 orderby fleetMemberSkills.InBoostShip descending,
                                fleetMemberSkills.FleetCommand descending,
								fleetMemberSkills.WingCommand descending,
								fleetMemberSkills.Leadership descending,
								fleetMemberSkills.MiningForeman descending,
								fleetMemberSkills.MiningDirector descending
							 select fleetMemberSkills).ToList();
			}

            if (fleetInfo.Count == 0)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "We have no info on fleet members; not moving members.");
                return;
            }

		    var wingCommandersNeeded = 0;

            if (fleetSize > 10)
                wingCommandersNeeded++;
            if (fleetSize > 51)
                wingCommandersNeeded++;
            if (fleetSize > 102)
                wingCommandersNeeded++;
            if (fleetSize > 153)
                wingCommandersNeeded++;
            if (fleetSize > 204)
                wingCommandersNeeded++;

            var needFleetCommander = wingCommandersNeeded > 1;

            if (needFleetCommander)
            {
                var bestFleetCommanderResult = fleetInfo.First();
                if (!fleetMembersById.ContainsKey(bestFleetCommanderResult.SendingFleetMemberCharId))
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Best fleet commander is not yet in fleet; waiting.");
                    return;
                }

                var bestFleetCommander = fleetMembersById[bestFleetCommanderResult.SendingFleetMemberCharId];
                if (bestFleetCommander.Role != RoleFleetCommander)
                {
                    var currentFleetCommander = fleetMembersById.Values.FirstOrDefault(
                        fleetMember => fleetMember.Role == RoleFleetCommander);
                    if (currentFleetCommander != null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Moving current fleet commander out of fleet commander position.");
                        //I need to have some means of tracking the capacity of each wing / squad so I can determine where to move this guy.
                        MoveFleetMemberToFirstSquadWithRoom(currentFleetCommander);
                        return;
                    }

                    LogMessage(methodName, LogSeverityTypes.Debug, "Moving fleet member {0} to fleet commander.", bestFleetCommander.CharID);
                    bestFleetCommander.MoveToFleetCommander();
                    return;
                }
            }

            //If I'm missing wings, create them
            if (wings.Count < wingCommandersNeeded)
            {
                LogMessage(methodName, LogSeverityTypes.Debug, "We have {0} wings and need {1}; creating one.", wings.Count, wingCommandersNeeded);
                Metatron.MeCache.Me.Fleet.CreateWing();
                return;
            }

            //Move any wing commanders into ideal positions
		    var wingCommanderInfoEntries = fleetInfo.Skip(1).Take(wingCommandersNeeded).ToList();

            var squadMemberExclusions = 0;
            if (needFleetCommander)
                squadMemberExclusions++;
            squadMemberExclusions += wingCommandersNeeded;

            var squadPositionsNeeded = fleetSize - squadMemberExclusions;
            var squadsNeeded = (int)Math.Ceiling((double)squadPositionsNeeded / 10);

            var wingsWithFullSquads = squadsNeeded / 5;
            var squadsInPartialWing = squadsNeeded % 5;

            var squadCommanderInfoEntries = fleetInfo.Skip(needFleetCommander ? 1 : 0).Skip(wingCommandersNeeded).Take(squadsNeeded);

            for (var index = 0; index < wings.Count(); index++)
            {
                var currentWingId = wings[index];

                if (wingCommanderInfoEntries.Any())
                {
                    var wingCommanderInfoEntry = wingCommanderInfoEntries[index];

                    if (!fleetMembersById.ContainsKey(wingCommanderInfoEntry.SendingFleetMemberCharId))
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug,
                                   "Ideal member for position {0} {1} is not yet in fleet; waiting.",
                                   RoleWingCommander, index + 1);
                        return;
                    }

                    var bestWingCommander = fleetMembersById[wingCommanderInfoEntry.SendingFleetMemberCharId];
                    if (bestWingCommander.Role != RoleWingCommander || bestWingCommander.WingID != currentWingId)
                    {
                        //Find the current wing commander for the wing this wing commander is intended for, e.g. if this is the 2nd wing commander, check wing 2 commander
                        var currentWingCommander = fleetMembersById.Values.FirstOrDefault(
                            fleetMember =>
                            fleetMember.Role == RoleWingCommander && fleetMember.WingID == currentWingId);
                        if (currentWingCommander != null)
                        {
                            LogMessage(methodName, LogSeverityTypes.Debug,
                                       "Moving current wing {0} commander out of position.",
                                       currentWingId);
                            //I need to have some means of tracking the capacity of each wing / squad so I can determine where to move this guy.
                            MoveFleetMemberToFirstSquadWithRoom(currentWingCommander);
                            return;
                        }

                        LogMessage(methodName, LogSeverityTypes.Debug, "Moving member {0} to wing {1} commander.",
                                   bestWingCommander.CharID, currentWingId);
                        bestWingCommander.MoveToWingCommander(currentWingId);
                        return;
                    }
                }

                //Get Squads for this wing and move members into it as necessary
                squads = Metatron.MeCache.Me.Fleet.GetSquads(currentWingId);
                
                var squadsForThisWing = index < wingsWithFullSquads ? 5 : squadsInPartialWing;
                if (squads.Count < squadsForThisWing)
                {
                    LogMessage(methodName, LogSeverityTypes.Debug, "Creating squad in wing {0}.", currentWingId);
                    Metatron.MeCache.Me.Fleet.CreateSquad(currentWingId);
                    return;
                }

                //Iterate squads in this wing and move someone to squad commander as necessary
                var squadInfoForCurrentWing = squadCommanderInfoEntries.Skip((index - 1)*5).Take(5).ToList();

                for (var squadIndex = 0; squadIndex < squadInfoForCurrentWing.Count; squadIndex++)
                {
                    var squadCommanderInfo = squadInfoForCurrentWing[squadIndex];

                    if (!fleetMembersById.ContainsKey(squadCommanderInfo.SendingFleetMemberCharId))
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Ideal member {0} for position {1} is not yet in fleet; waiting.",
                            squadCommanderInfo.SendingFleetMemberCharId, RoleSquadCommander);
                        return;
                    }
                    
                    var currentSquadId = squads[squadIndex];
                    var bestSquadCommander = fleetMembersById[squadCommanderInfo.SendingFleetMemberCharId];
                    if (bestSquadCommander.Role == RoleSquadCommander && bestSquadCommander.SquadID == currentSquadId)
                        continue;

                    var currentSquadCommander = fleetMembersById.Values.FirstOrDefault(
                        fleetMember => fleetMember.Role == RoleSquadCommander && fleetMember.SquadID == currentSquadId);

                    if (currentSquadCommander != null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Debug, "Moving current squad {0} commander out of position.", 
                                   currentSquadId);
                        //I need to have some means of tracking the capacity of each wing / squad so I can determine where to move this guy.
                        MoveFleetMemberToFirstSquadWithRoom(currentSquadCommander);
                        return;
                    }

                    LogMessage(methodName, LogSeverityTypes.Debug, "Moving member {0} to squad {1} commander.",
                               bestSquadCommander.CharID, currentWingId);
                    bestSquadCommander.MoveToSquadCommander(currentWingId, currentSquadId);
                }
            }
		}

        private void MoveFleetMemberToFirstSquadWithRoom(FleetMember fleetMember)
        {
            var methodName = "MoveFleetMemberToFirstSquadWithRoom";
            LogTrace(methodName, "{0}", fleetMember.ID);

            var wings = Metatron.MeCache.Me.Fleet.GetWings();
            var squadsWithRoom = new List<long>();
            var wingsBySquadId = new Dictionary<long, long>();

            foreach (var wing in wings)
            {
                var squads = Metatron.MeCache.Me.Fleet.GetSquads(wing);
                foreach (var squad in squads)
                {
                    wingsBySquadId.Add(squad, wing);

                    var freeSpaceInSquad = 10 - Metatron.MeCache.FleetMembersById.Values.Count(fm => fm.SquadID == squad);
                    if (freeSpaceInSquad > 0)
                        squadsWithRoom.Add(squad);
                }
            }

            if (squadsWithRoom.Count == 0)
            {
                ExpandFleet(wings);
                return;
            }

            var squadToMoveTo = squadsWithRoom.Last();
            var wingToMoveTo = wingsBySquadId[squadToMoveTo];

            LogMessage(methodName, LogSeverityTypes.Debug, "Moving fleet member {0} to wing {1}, squad {2}.",
                       fleetMember.CharID, wingToMoveTo, squadToMoveTo);
            fleetMember.Move(wingToMoveTo, squadToMoveTo);
        }

	    private void ExpandFleet(ICollection<long> wings)
	    {
	        var methodName = "ExpandFleet";
            LogTrace(methodName, "Wings: {0}", wings.Count);

	        var fullWings = 0;
            foreach (var wing in wings)
            {
                var squads = Metatron.MeCache.Me.Fleet.GetSquads(wing);

                var fullSquads = squads.Count(
                    squad => Metatron.MeCache.FleetMembers.Count(fleetMember => fleetMember.SquadID == squad) == 10);

                if (fullSquads < 5)
                {
                    LogMessage(methodName, LogSeverityTypes.Standard, "Creating a new squad in wing {0}.", wing);
                    Metatron.MeCache.Me.Fleet.CreateSquad(wing);
                    return;
                }
            }

            if (fullWings < 5)
            {
                Metatron.MeCache.Me.Fleet.CreateWing();
                LogMessage(methodName, LogSeverityTypes.Standard, "Creating a new wing.");
            }
	    }

	    private void OnFleetNeedSkills(object sender, BaseEventArgs e)
		{
            var methodName = "OnFleetNeedSkills";
			LogTrace(methodName);

			lock (this)
			{
				_infoRequestsToProcess.Add(e);
			}
		}

		private void OnFleetAcceptInvitation(object sender, FleetAcceptInvitationEventArgs e)
		{
            var methodName = "OnFleetAcceptInvitation";
			LogTrace(methodName);

			lock (this)
			{ 
				_acceptFleetInviteNotificationsToProcess.Add(e);
			}
		}

		private void OnFleetMemberSkillsReceived(object sender, FleetMemberSkillsReceivedEventArgs e)
		{
            var methodName = "OnFleetMemberSkillsReceived";
			LogTrace(methodName, "SendingFleetMemberId: {0}, InBoostShip: {1}, FleetCommand: {2}, WingCommand: {3}, Leadership: {4}, MiningForeman: {5}, MiningDirector: {6}",
                e.SendingFleetMemberCharId, e.InBoostShip, e.FleetCommand, e.WingCommand, e.Leadership, e.MiningForeman, e.MiningDirector);

			lock (this)
			{
				if (_fleetInfoByCharId.ContainsKey(e.SendingFleetMemberCharId))
				{
					_fleetInfoByCharId[e.SendingFleetMemberCharId] = e;
				}
				else
				{
					_fleetInfoByCharId.Add(e.SendingFleetMemberCharId, e); 
				}
			}
		}

		private void DetermineFleetSkills()
		{
            var methodName = "DetermineFleetSkills";
			LogTrace(methodName);

			//Get the levels for all the skills, skill names will probably need corrected
			WingCommandLevel = GetSkillLevel("Wing Command");
			FleetCommandLevel = GetSkillLevel("Fleet Command");
			MiningForemanLevel = GetSkillLevel("Mining Foreman");
			MiningDirectorLevel = GetSkillLevel("Mining Director");
			ArmoredWarfareLevel = GetSkillLevel("Armored Warfare");
			SkirmishWarfareLevel = GetSkillLevel("Skirmish Warfare");
			InformationWarfareLevel = GetSkillLevel("Information Warfare");
			SiegeWarfareLevel = GetSkillLevel("Siege Warfare");
			WarfareLinkSpecialistLevel = GetSkillLevel("Warfare Link Specialist");
			LeadershipLevel = GetSkillLevel("Leadership");
		}

		private int GetSkillLevel(string skillName)
		{
            var methodName = "GetSkillLevel";
			LogTrace(methodName, "Skill name: {0}", skillName);

            var skill = Metatron.MeCache.Me.Skill(skillName);
			return !LavishScriptObject.IsNullOrInvalid(skill) ? skill.Level : 0;
		}
	}
    // ReSharper restore PossibleMultipleEnumeration
    // ReSharper restore StringIndexOfIsCultureSpecific.1
}
