using System;
using System.Collections.Generic;
using Metatron.Core;
using EVE.ISXEVE;
using System.Linq;
using Metatron.ActionModules;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;
using IShip = Metatron.Core.IShip;
using Metatron.Core.EventCommunication;

namespace Metatron.BehaviorModules
{
	internal class Ratting : BehaviorBase
	{
		private RattingStates _rattingState = RattingStates.Idle;
		private bool _npcCheck, _pcCheck, _isWipingThisSpawn, _doWarpInCheck = true;

	    private int PulsesToWaitForRats = 5;
		private int _warpInCheckCounter = 5;

	    private long? _selectedAnomalyId;
	    private ChangeAnomalyStates _changeAnomalyState = ChangeAnomalyStates.BookmarkSiteForSalvage;

	    private readonly List<IEntityWrapper> _ratNpcEntities = new List<IEntityWrapper>();
		private readonly List<IEntityWrapper> _ratPcEntities = new List<IEntityWrapper>();

		private readonly List<IEntityWrapper> _specialNpcs = new List<IEntityWrapper>();
		private readonly List<IEntityWrapper> _ewarNpcs = new List<IEntityWrapper>();
		private readonly List<Int64> _doNotKillEntityIDs = new List<Int64>();

		private readonly RandomWaitObject _randomWaitObject;

	    private readonly ISocial _social;
	    private readonly IMeCache _meCache;
	    private readonly IBookmarks _bookmarks;
	    private readonly ISalvageConfiguration _salvageConfiguration;
	    private readonly IRattingConfiguration _rattingConfiguration;
	    private readonly IAnomalyProvider _anomalyProvider;
	    private readonly IEntityProvider _entityProvider;
	    private readonly IAnomalyClaimTracker _anomalyClaimTracker;
	    private readonly ISafespots _safespots;
	    private readonly IShip _ship;
	    private readonly IAsteroidBelts _asteroidBelts;
	    private readonly IMovementConfiguration _movementConfiguration;
	    private readonly IAlerts _alerts;
	    private readonly ITargetQueue _targetQueue;
	    private readonly IAttackers _attackers;

	    private readonly IMovement _movement;

	    /// <summary>
		/// Npcs with these tags in their name are known warp disrupt / ewar npcs.
		/// </summary>
		string KnownEwarPointTargetTags = "Ratting_KnownEwarPointTargetTags";
		IEnumerable<string> _knownEwarPointTargetTags
		{
			get
			{
				return (List<string>)_cachedResourcesByKeys[KnownEwarPointTargetTags];
			}
		}

		public Ratting(ISocial social, IMeCache meCache, IBookmarks bookmarks, ISalvageConfiguration salvageConfiguration, IRattingConfiguration rattingConfiguration, IAnomalyProvider anomalyProvider,
            IEntityProvider entityProvider, IAnomalyClaimTracker anomalyClaimTracker, ISafespots safespots, IMovement movement, IShip ship, IAsteroidBelts asteroidBelts, IMovementConfiguration movementConfiguration, 
            IAlerts alerts, ITargetQueue targetQueue, IAttackers attackers)
		{
		    _social = social;
		    _meCache = meCache;
		    _bookmarks = bookmarks;
		    _salvageConfiguration = salvageConfiguration;
		    _rattingConfiguration = rattingConfiguration;
		    _anomalyProvider = anomalyProvider;
		    _entityProvider = entityProvider;
		    _anomalyClaimTracker = anomalyClaimTracker;
		    _safespots = safespots;
		    _movement = movement;
		    _ship = ship;
		    _asteroidBelts = asteroidBelts;
		    _movementConfiguration = movementConfiguration;
		    _alerts = alerts;
		    _targetQueue = targetQueue;
		    _attackers = attackers;

		    BehaviorManager.BehaviorsToPulse.Add(BotModes.Ratting, this);
			ModuleName = "Ratting";
			IsEnabled = true;
			PulseFrequency = 2;

			var knownEwarPointTargetTags = new List<string> {
				"Dire Guristas", "Arch Angel Hijacker", "Arch Angel Outlaw", "Arch Angel Rogue",
				"Arch Angel Thug", "Sansha's Loyal", "Guardian Agent", "Guardian Initiate",
				"Guardian Scout", "Guardian Spy", " Watchman", " Patroller", 
				"Elder Blood Upholder", "Elder Blood Worshipper", "Elder Blood Follower", "Elder Blood Herald",
				"Blood Wraith", "Blood Disciple", "Strain " 
			};
			_cachedResourcesByKeys.Add(KnownEwarPointTargetTags, knownEwarPointTargetTags);

			_randomWaitObject = new RandomWaitObject(ModuleName);
			_randomWaitObject.AddWait(new KeyValuePair<int, int>(30, 70), 5);
			_randomWaitObject.AddWait(new KeyValuePair<int, int>(5, 15), 10);
		}

		public override void Pulse()
		{
			if (!ShouldPulse())
				return;

			//if I'm moving in any way other than approaching something, don't do any other logic.
			if (_movement.IsMoving && (_movement.MovementType != MovementTypes.Approach))
			{
				//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
				//methodName, "Not pulsing due to moving in some way other than approach."));
				return;
			}

		    if (!_ship.IsInventoryReady) return;

			StartPulseProfiling();

			//if in space, repopulate special npcs
			if (_meCache.InSpace)
			{
			    GetEntities();

			    RepopulateSpecialNpcs();
				RepopulateEwarNpcs();
				//Do the NPC check
				_npcCheck = CheckNpcs();
				_pcCheck = CheckPcs();
			}

			SetPulseState();

			ProcessPulseState();

			EndPulseProfiling();
		}

	    private void GetEntities()
	    {
	        var methodName = "GetEntities";
	        LogTrace(methodName);

	        ClearEntities();

	        _ratNpcEntities.AddRange(
	            Core.Metatron.EntityProvider.EntityWrappers.Where(
                    entity => entity.IsNPC && entity.CategoryID == (int) CategoryIDs.Entity && !_attackers.IsConcordTarget(entity.GroupID) &&
                    _attackers.IsRatTarget(entity) && entity.Distance < (int) Ranges.Warp));

	        _ratPcEntities.AddRange(
	            Core.Metatron.EntityProvider.EntityWrappers.Where(
	                entity => entity.IsPC && entity.CategoryID == (int) CategoryIDs.Ship && entity.ID != _meCache.ShipId));
	    }

	    private void ClearEntities()
	    {
	        _ratNpcEntities.Clear();
	        _ratPcEntities.Clear();
	    }

	    protected override void SetPulseState()
		{
			var methodName = "SetPulseState";
			LogTrace(methodName);

			//If defense has us fleeing we probably shouldn't be doing anything.);
			if (Core.Metatron.Defense.IsFleeing)
			{
				_rattingState = RattingStates.Defend;
				return;
			}

			//Ok, not fleeign/defending. Check ammo status.
			/*if (!_ship.IsAmmoAvailable)
			{
				_rattingState = RattingStates.Rearm; 
				return;
			}*/

			//If it's an error state, return.
	        if (_rattingState == RattingStates.Error)
	        {
	            if (HasMaxRuntimeExpired()) return;

	            if (_rattingConfiguration.IsAnomalyMode)
	            {
	                var anomalies = GetAvailableAnomalies();
                    if (anomalies.Any(b => _meCache.ToEntity.DistanceTo(b.X, b.Y, b.Z) >= (int)Ranges.Warp * 2))
	                {
                        LogMessage(methodName, LogSeverityTypes.Standard, "New anomalies matching those selected were detected.");
	                    _rattingState = RattingStates.ChangeBelts;
	                }
	            }
	            return;
	        }

	        //if I'm in station, I should probably focus on getting to a belt.
			if (_meCache.InStation)
			{
				_rattingState = RattingStates.ChangeBelts;
			}
			else
			{
				//If the NPC check and player check passed, kill shit
				if (_npcCheck && _pcCheck)
				{
					_rattingState = RattingStates.KillRats;
				}
				else
				{
				    if (HasMaxRuntimeExpired())
				        _rattingState = RattingStates.Error;
				    else
				    {
                        //Either no rats or a player here, change belts
                        _rattingState = RattingStates.ChangeBelts;
				    }
				}
			}
		}

		protected override void ProcessPulseState()
		{
			var methodName = "ProcessPulseState";
			LogTrace(methodName);

			//Switch the state and handle it
			switch (_rattingState)
			{
				case RattingStates.Idle:
					//Do nothing during idle.
					break;
				case RattingStates.Defend:
					//Again do nothing; we're fleeing or hiding.
			        ClearEntities();
					break;
				case RattingStates.Rearm:
					//Add later
					break;
				case RattingStates.KillRats:
					KillRats();
					break;
				case RattingStates.ChangeBelts:
					ChangeBelt();
					CallSalvager();
					break;
				case RattingStates.Error:
			        if (_safespots.IsSafe()) return;

			        var safespot = _safespots.GetSafeSpot();
			        _movement.QueueDestination(safespot);
					break;
			}
		}

		protected override void _processCleanupState()
		{
			
		}

		protected override void _setCleanupState()
		{
			
		}

        private void CallSalvager()
        {
            var methodName = "CallSalvager";
            LogTrace(methodName);

            if (!_salvageConfiguration.CreateSalvageBookmarks)
            {
                return;
            }
            else
            {
                var salvageBookmarks = Core.Metatron.BookMarkCache.GetBookMarksStartingWith(_movementConfiguration.SalvagingPrefix, false);
                LogTrace(methodName, $"Total: {salvageBookmarks.Count}");
                foreach (var bookMark in salvageBookmarks)
                {
                    if (bookMark.CreatorId == Core.Metatron.MeCache.Me.ID)
                    {
                        LogTrace(methodName, $"IsMine: {bookMark.Id}");
                        var bookmarkSafetyEvent = new SalvageBookmarkReadyEventArgs(_logging, bookMark.Id, Core.Metatron.MeCache.Name, Core.Metatron.MeCache.Me.ID, bookMark.SolarSystemId);
                        Core.Metatron.EventCommunications.SalvageBookmarkReadyEvent.SendEvent(bookmarkSafetyEvent);
                    }
                }
            }
        }

        private void KillRats()
		{
			var methodName = "KillRats";
			LogTrace(methodName);

			//If no rats are present, just return early.
		    if (_ratNpcEntities.Count == 0)
		    {
		        LogMessage(methodName, LogSeverityTypes.Debug, "No NPCs detected.");
		        return;
		    }

			//Build a new list<entity> to queue
			var entities = new List<IEntityWrapper>();

			//if I'm not chaining, just queue them all.
			if (!ShouldChainSpawns)
			{
				LogMessage(methodName, LogSeverityTypes.Debug, "Should not chain spawns, wiping this spawn.");
				entities.AddRange(_ratNpcEntities);
				_isWipingThisSpawn = true;
			}
			else
			{
				//If it's an existing chain (any entities are contained in the do not kill list),
				//queue up ALL entities not in the list.
				var isExistingChain = _ratNpcEntities.Any(entity => _doNotKillEntityIDs.Contains(entity.ID));

				if (isExistingChain)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "This spawn is part of an existing chain, killing battleships.");
					entities.AddRange(_ratNpcEntities.Where(entity => !_doNotKillEntityIDs.Contains(entity.ID)));
				}
				else
				{
					//Ok, -not- an existing chain. If there are special rats or bad rats, don't chain.
					if (_specialNpcs.Count > 0 || _ewarNpcs.Count > 0)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Special or E-War targets were present in this spawn, not chaining.");

						_isWipingThisSpawn = true;
						entities.AddRange(_ratNpcEntities);
					}
					else
					{
						//No special rats - calculate the bounty total.
						Int64 bountyTotal = 0;

						//Keep track of battleships as ToEntity.Group is fucking hard on perf.
						var battleships = new List<Int64>();
						//Iterate all rats, looking for battleships and calculating the total bounty.
						foreach (var entity in _ratNpcEntities.Where(entity => entity.ToEntity.Group.Contains("Battleship")))
						{
							battleships.Add(entity.ID);

							var bountyForShip = entity.Bounty == 0
							                    	? Core.Metatron.NpcBountyCache.GetBountyForNpc(entity.Name)
							                    	: (long)entity.Bounty;
							bountyTotal += bountyForShip;

							LogMessage(methodName, LogSeverityTypes.Debug,
								"Rat \"{0}\" ({1}) is a battleship with known bounty of {2}.", entity.Name, entity.ID, bountyForShip);
						}

						//If the bounty is above threshold...
						if (bountyTotal >= _rattingConfiguration.MinimumChainBounty)
						{
							LogMessage(methodName, LogSeverityTypes.Debug, "Calculated bounty total {0} was above threshold {1}, chaining the spawn.",
								bountyTotal, _rattingConfiguration.MinimumChainBounty);
							//Chain it! Queue battleships, donotkill the rest.
							foreach (var entity in _ratNpcEntities)
							{
								if (battleships.Contains(entity.ID))
								{
									entities.Add(entity);
								}
								else
								{
									_doNotKillEntityIDs.Add(entity.ID);
								}
							}
						}
						else
						{
							LogMessage(methodName, LogSeverityTypes.Debug, "Calculated bounty total {0} was not above threshold {1}, wiping the spawn.",
								bountyTotal, _rattingConfiguration.MinimumChainBounty);
							//Shame. Wipe it.
							entities.AddRange(_ratNpcEntities);
							_isWipingThisSpawn = true;
						}
					}
				}
			}

			//Queue the entities
			if (entities.Count > 0)
			{
				QueueEntities(entities);
			}
		}

		private void ChangeBelt()
		{
			var methodName = "ChangeBelt";
			LogTrace(methodName);

			if (_randomWaitObject.ShouldWait())
				return;
			
			//if I'm in station, I need to undock.
			if (_meCache.InStation)
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "Undocking to move to a belt.");
				_movement.QueueDestination(new Destination(DestinationTypes.Undock, string.Empty));
				return;
			}

			//reset the wipe stuff
			_isWipingThisSpawn = false;
			//Also reset the warp-in check
			_doWarpInCheck = true;
			_warpInCheckCounter = PulsesToWaitForRats;

			//gotta work differently depending on type of belts I'm using.
			//Only allow reseting of IsBeltEmpty if not in anomaly mode
			_movement.ClearDestinations(true);
            if (_rattingConfiguration.IsAnomalyMode)
            {
                MoveToNextAnomaly();
            }
            else
            {
                MoveToNextAsteroidBelt();
            }
		}

        private void MoveToNextAnomaly()
        {
            var methodName = "MoveToNextAnomaly";
            LogTrace(methodName);

            switch (_changeAnomalyState)
            {
                case ChangeAnomalyStates.BookmarkSiteForSalvage:
                    BookmarkAnomalyForSalvage();

                    //Clear the selected anomaly
                    _selectedAnomalyId = null;

                    _changeAnomalyState = ChangeAnomalyStates.SelectAnomaly;
                    goto case ChangeAnomalyStates.SelectAnomaly;
                case ChangeAnomalyStates.SelectAnomaly:
                    {
                        var selectedAnomaly = SelectAnomaly();

                        //If none, safe up.
                        if (selectedAnomaly == null)
                        {
                            LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not find an anomaly matching selected anomalies.");
                            _rattingState = RattingStates.Error;

                            //Reset the ChangeAnomalyState
                            _changeAnomalyState = ChangeAnomalyStates.BookmarkSiteForSalvage;
                            return;
                        }

                        //Claim the anomaly
                        _anomalyClaimTracker.ClaimAnomaly(selectedAnomaly.ID);

                        _selectedAnomalyId = selectedAnomaly.ID;
                        _changeAnomalyState = ChangeAnomalyStates.ValidateAnomaly;
                        break;
                    }
                case ChangeAnomalyStates.ValidateAnomaly:
                    {
                        if (_selectedAnomalyId == null)
                        {
                            LogMessage(methodName, LogSeverityTypes.Debug, "Error: Cannot validate a non-existent anomaly.");
                            _changeAnomalyState = ChangeAnomalyStates.SelectAnomaly;
                            return;
                        }

                        //make sure there are no new claims on the selected anomaly
                        if (_anomalyClaimTracker.IsAnomalyClaimedByOther(_selectedAnomalyId.Value))
                        {
                            LogMessage(methodName, LogSeverityTypes.Standard, "Another player has a better claim to anomaly ID {0}. Selecting another anomaly.",
                                _selectedAnomalyId.Value);

                            _selectedAnomalyId = null;
                            _changeAnomalyState = ChangeAnomalyStates.SelectAnomaly;
                            goto case ChangeAnomalyStates.SelectAnomaly;
                        }

                        _changeAnomalyState = ChangeAnomalyStates.MoveToAnomaly;
                        goto case ChangeAnomalyStates.MoveToAnomaly;
                    }
                case ChangeAnomalyStates.MoveToAnomaly:
                    {
                        var selectedAnomaly = _anomalyProvider.GetAnomalies().FirstOrDefault(a => a.ID == _selectedAnomalyId.Value);

                        if (selectedAnomaly == null)
                        {
                            LogMessage(methodName, LogSeverityTypes.Standard, "Error: No anomaly exists for selected anomaly ID {0}.", _selectedAnomalyId.Value);
                            _changeAnomalyState = ChangeAnomalyStates.SelectAnomaly;

                            _selectedAnomalyId = null;

                            goto case ChangeAnomalyStates.SelectAnomaly;
                        }

						var warpToDistance = _rattingConfiguration.WarpInDistance * 1000;

                        LogTrace(methodName, $"Distance: {warpToDistance}");

                        var destination = new Destination(DestinationTypes.CosmicAnomaly)
						{
							SystemAnomalyId = selectedAnomaly.ID,
							WarpToDistance = warpToDistance,
                        };
                        LogMessage(methodName, LogSeverityTypes.Standard, "Moving to cosmic anomaly \"{0} - {1}\" ({2}).",
                            selectedAnomaly.Name, selectedAnomaly.DungeonName, selectedAnomaly.ID);
                        _movement.QueueDestination(destination);

                        _changeAnomalyState = ChangeAnomalyStates.BookmarkSiteForSalvage;
                        break;
                    }
            }
        }

	    private SystemAnomaly SelectAnomaly()
	    {
	        var anomalies = GetAvailableAnomalies();

	        //Grab the first one I'm not at.
	        var nearbyAnomaly = anomalies.FirstOrDefault(b => _meCache.ToEntity.DistanceTo(b.X, b.Y, b.Z) < (int) Ranges.Warp*2);
	        var anomaly = anomalies.FirstOrDefault(b => (nearbyAnomaly == null || b.ID != nearbyAnomaly.ID) &&
	                                                    _meCache.ToEntity.DistanceTo(b.X, b.Y, b.Z) >= (int) Ranges.Warp*2);
	        return anomaly;
	    }

	    private void BookmarkAnomalyForSalvage()
	    {
	        var wrecksExist = _entityProvider.EntityWrappers.Any(e => e.GroupID == (int) GroupIDs.Wreck);
	        if (wrecksExist && !_ratNpcEntities.Any() && _salvageConfiguration.CreateSalvageBookmarks)
	        {
				var wreck = _entityProvider.EntityWrappers.Where(e => e.GroupID == (int)GroupIDs.Wreck).First();
                //If we're at a completed anomaly and we can create salvage bookmarks, do so!
                _bookmarks.CreateSalvagingBookmark(wreck);
	        }
	    }

	    private void MoveToNextAsteroidBelt()
	    {
	        var methodName = "MoveToNextAsteroidBelt";
	        LogTrace(methodName);

	        _asteroidBelts.ChangeBelts(true, true);

	        if (_movementConfiguration.OnlyUseBeltBookmarks
	                ? _asteroidBelts.AllBookMarkedBeltsEmpty
	                : _asteroidBelts.AllBeltsEmpty)
	        {
	            LogMessage(methodName, LogSeverityTypes.Standard, "Error; all belts are empty.");
	            _rattingState = RattingStates.Error;
	            return;
	        }

			var warpToDistance = _rattingConfiguration.WarpInDistance * 1000;

            if (_movementConfiguration.OnlyUseBeltBookmarks)
	        {
	            if (_asteroidBelts.CurrentBookMarkedBelt != null)
	            {
                    LogTrace(methodName, $"Distance: {warpToDistance}");
                    _movement.QueueDestination(new Destination(
																DestinationTypes.BookMark,
																_asteroidBelts.CurrentBookMarkedBelt.Id,
																warpToDistance));
	            }
	            else
	            {
	                _rattingState = RattingStates.Error;
	            }
	        }
	        else
	        {
	            if (_asteroidBelts.CurrentBelt != null)
	            {
					LogTrace(methodName, $"Distance: {warpToDistance}");
	                _movement.QueueDestination(new Destination(
	                                                            DestinationTypes.Entity,
	                                                            _asteroidBelts.CurrentBelt.Id, warpToDistance));
	            }
	            else
	            {
	                _rattingState = RattingStates.Error;
	            }
	        }
	    }

	    private void QueueEntities(IEnumerable<IEntityWrapper> entitiesToQueue)
		{
			var methodName = "QueueEntities";
			LogTrace(methodName);

			//Iterate all entities
			foreach (var entity in entitiesToQueue.Where(entity => !_targetQueue.IsQueued(entity.ID)))
			{
				//If this is a special target, see if we need to play the warning.
				if (_specialNpcs.Contains(entity))
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Found special NPC \"{0}\" ({1}).", 
                        entity.Name, entity.ID);
					_alerts.FactionSpawn(entity.Name);
					_targetQueue.EnqueueTarget(entity.ID, (int)TargetPriorities.Kill_Other, TargetTypes.Kill);
				}
					//next check ewar results
				else if (_ewarNpcs.Contains(entity))
				{
					//Nuke it with great prejudice.
					_targetQueue.EnqueueTarget(entity.ID, (int)TargetPriorities.Kill_OtherElectronicWarfare, TargetTypes.Kill);
				}
				else
				{
					//Normal killage.
					if (_attackers.QueueTargetsByEntityId.ContainsKey(entity.ID))
					{
						var queueTarget = _attackers.QueueTargetsByEntityId[entity.ID];
						_targetQueue.EnqueueTarget(entity.ID, queueTarget.Priority, TargetTypes.Kill);
					}
					else
					{
					    var targetPriority = _attackers.GetTargetPriority(entity);
                        _targetQueue.EnqueueTarget(entity.ID, targetPriority, TargetTypes.Kill);
					}
				}
			}
		}

		/// <summary>
		/// Determine if there are sufficient NPCs to stay and fight, meaning:
		/// 1) Rats are present,
		/// 2) Not chaning -or- (special/faction/ewar spawn or bad chain [meaning more than just one-ship-type and not on do-not-kill list])
		/// </summary>
		private bool CheckNpcs()
		{
			var methodName = "CheckNpcs";
			LogTrace(methodName);

			//Firstly determine if rats are present
			if (_ratNpcEntities.Count > 0)
			{
				//Un-set the do warpin check for this belt
				_doWarpInCheck = false;

				//Alright, if I'm not chaining or I'm wiping this spawn, return true.
				if (!ShouldChainSpawns || _isWipingThisSpawn)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Shouldn't chain spawns or we're wiping this spawn, npc check passed.");
					return true;
				}

				//Ok, chaining. Is a special spawn present?
				if (_specialNpcs.Count > 0)
				{
					//Yep, this means kill things.
					LogMessage(methodName, LogSeverityTypes.Debug, "Special npc \"{0}\" ({1}) detected, npc check passed.",
					           _specialNpcs[0].Name, _specialNpcs[0].ID);
					return true;
				}

				//No special spawn. How many entities are on the do not kill list?
				var entitiesOnDoNotKillList = _ratNpcEntities.Count(entity => _doNotKillEntityIDs.Contains(entity.ID));

				//If the # entities on the do not kill list matches the
				//# entities in the rat cache, we have nothing left to kill.
				if (entitiesOnDoNotKillList >= _ratNpcEntities.Count)
				{
					LogMessage(methodName, LogSeverityTypes.Debug,
					           "All present entities are on the do not kill list, npc check failed.");
					return false;
				}

				//First, check if we've got more than one type of rat.
				var moreThanOneType = false;
				var lastType = 0;
				foreach (var entity in _ratNpcEntities)
				{
					if (lastType == 0)
					{
						lastType = entity.TypeID;
						continue;
					}

					if (lastType == entity.TypeID)
						continue;

					moreThanOneType = true;
					break;
				}

				//If there's more than one type of rat, we can kill something.
				//TODO: Where the hell am I checking rat bounty value?
				if (moreThanOneType)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "More than one type of rat detected, NPC check passed.");
					//Set the _chainBelt flag here, do NOT unset it ANYWHERE but ChangeBelts
					return true;
				}
				//This statement should never be hit but it never hurts to be sure
				if (_isWipingThisSpawn)
				{
					LogMessage(methodName, LogSeverityTypes.Debug,
					           "One type of rat but wiping this spawn, NPC check failed but not preserving rats.");
					return true;
				}

				//Only one type of rat, chain in progress or partial spawn. Add any not present on the do not kill list to the do not kill list.
				foreach (var entity in _ratNpcEntities.Where(entity => !_doNotKillEntityIDs.Contains(entity.ID)))
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Only detected one type of rat, probable chain in progress, adding entity \"{0}\" ({1}) to the do not kill list.",
					           entity.Name, entity.ID);
					_doNotKillEntityIDs.Add(entity.ID);
				}

				LogMessage(methodName, LogSeverityTypes.Debug, "Only one type of rat detected, probably a chain in progress. NPC check failed.");
				return false;
			}
			
			if (_doWarpInCheck)
			{
				//If nothing's here delay a bit for warp-ins
				if (!_rattingConfiguration.IsAnomalyMode && _asteroidBelts.IsAtAsteroidBelt() && --_warpInCheckCounter >= 0)
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Waiting a pulse for rats...");
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Determine if there are any PCs with us. Returns true if good to stay, false otherwise.
		/// </summary>
		/// <returns></returns>
		private bool CheckPcs()
		{
			var methodName = "CheckPcs";
			LogTrace(methodName);

			//Return true if there are more than one result since we'll likely show up in the results.
		    if (_ratPcEntities.Count > 0 && _rattingState != RattingStates.KillRats)
		    {
		        LogMessage(methodName, LogSeverityTypes.Debug, "Player check failed - there are currently {0} players in the belt. Players: \n{1}", 
                    _ratPcEntities.Count, string.Join("\n", _ratPcEntities.Select(e => string.Format("\"{0}\" ({1})", e.Name, e.ID))));

                //If we're in anomaly mode, claim the anomaly on their behalf
                if (_rattingConfiguration.IsAnomalyMode)
                {
					LogMessage(methodName, LogSeverityTypes.Debug, "We're in anomaly mode, we need to blacklist this anomaly");
                    var anomaly = _anomalyProvider.GetAnomalies()
                        .OrderBy(a => _meCache.ToEntity.DistanceTo(a.X, a.Y, a.Z))
                        .FirstOrDefault();
					var ratNpcEntity = _ratPcEntities.OrderBy(e => e.Distance).FirstOrDefault();
					var distanceToPlayer = _meCache.ToEntity.DistanceTo(ratNpcEntity.ID);
                    if (anomaly != null && distanceToPlayer < 2 * (int)Ranges.Warp)
                    {
						LogMessage(methodName, LogSeverityTypes.Debug, $"Claiming anomaly ${anomaly.Name} ({anomaly.ID}) [{anomaly.X},{anomaly.Y},{anomaly.Z}:{distanceToPlayer}] for player {ratNpcEntity.ID}");
                        //Get their entity and claim the anomaly on their behalf
                        _anomalyClaimTracker.ClaimAnomaly(anomaly.ID, _ratPcEntities.FirstOrDefault().ToEntity.ID);
                    }
                }

		        return false;
		    }

		    return true;
		}

		/// <summary>
		/// Clear and repopulate the list of special NPCs.
		/// </summary>
		private void RepopulateSpecialNpcs()
		{
			var methodName = "RepopulateSpecialNpcs";
			LogTrace(methodName);

			_specialNpcs.Clear();

			//Iterate all cached entities
			foreach (var entity in _ratNpcEntities)
			{
			    var groupID = entity.GroupID;

				if (_attackers.IsOfficer(groupID) || _attackers.IsHauler(groupID) || _attackers.IsCommander(groupID))
				{
					_specialNpcs.Add(entity);
				}
			}
		}

		/// <summary>
		/// Clear and repopulate the list of ewar NPCs.
		/// </summary>
		private void RepopulateEwarNpcs()
		{
			var methodName = "RepopulateEwarNpcs";
			LogTrace(methodName);

			_ewarNpcs.Clear();

			//Iterate all cached entities
			foreach (var entity in
				_ratNpcEntities.Where(entity => _knownEwarPointTargetTags.Any(s => entity.Name.Contains(s))))
			{
				_ewarNpcs.Add(entity);
			}
		}

        /// <summary>
        /// Get a list of anomalies matching those selected for running.
        /// </summary>
        /// <returns></returns>
        private IList<SystemAnomaly> GetAvailableAnomalies()
        {
            var anomalies = _anomalyProvider.GetAnomalies();

            var selectedAnomalyTypes = _rattingConfiguration.StatusByAnomalyType
                .Where(p => p.Second)
                .Select(p => p.First)
                .ToList();

            var selectedAnomalies = anomalies.Where(a =>
            {
                // Check if DungeonName contains any of the selectedAnomalyTypes
                return selectedAnomalyTypes.Any(type => a.DungeonName.Contains(type));
            })
                .Where(a => !_anomalyClaimTracker.IsAnomalyClaimedByOther(a.ID))
                .OrderByDescending(a =>
                {
                    // Find the first matching anomaly type from selectedAnomalyTypes for ordering
                    var matchingType = selectedAnomalyTypes.FirstOrDefault(type => a.DungeonName.Contains(type));
                    return selectedAnomalyTypes.IndexOf(matchingType);
                });

            return selectedAnomalies.ToList();
        }

        /// <summary>
        /// Determine if we're -really- chaining spawns
        /// </summary>
	    private bool ShouldChainSpawns
	    {
            get
            {
                //We can't chain if running anomalies
                if (_rattingConfiguration.IsAnomalyMode) return false;

                //If chaining isn't enabled, we shouldn't chain
                if (!_rattingConfiguration.ChainBelts) return false;

                //If we're set to only chain when alone, make sure we're alone
                if (_rattingConfiguration.OnlyChainWhenAlone)
                {
                    return _meCache.SolarSystemId >= 0 && _social.LocalPilots.Any();
                }

                return true;
            }
	    }

        private enum ChangeAnomalyStates
        {
            /// <summary>
            /// Bookmark the anomaly location for salvage
            /// </summary>
            BookmarkSiteForSalvage,
            /// <summary>
            /// Select a new anomaly
            /// </summary>
            SelectAnomaly,
            /// <summary>
            /// Validate the anomaly against other claims
            /// </summary>
            ValidateAnomaly,
            /// <summary>
            /// Move to the selected and validated anomaly
            /// </summary>
            MoveToAnomaly
        }
	}

	public enum RattingStates
	{
		Idle,
		Defend,
		Rearm,
		ChangeBelts,
		KillRats,
		Error
	}
}
