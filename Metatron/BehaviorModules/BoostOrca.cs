using System;
using Metatron.ActionModules;
using Metatron.Core;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;
using IShip = Metatron.Core.IShip;

namespace Metatron.BehaviorModules
{
	public class BoostOrca : BehaviorBase
	{
		private BoostOrcaStates _boostOrcaState = BoostOrcaStates.Idle;

	    private readonly IBookMarkCache _bookMarkCache;
	    private readonly IMiningConfiguration _miningConfiguration;
        private readonly IBookmarks _bookmarks;
	    private readonly ISafespots _safespots;
	    private readonly IMovement _movement;
	    private readonly IMainConfiguration _mainConfiguration;
	    private readonly IShip _ship;
	    private readonly IMeCache _meCache;

		public BoostOrca(IBookMarkCache bookMarkCache, IMiningConfiguration miningConfiguration, IBookmarks bookmarks, ISafespots safespots, IMovement movement, IMainConfiguration mainConfiguration, IShip ship, IMeCache meCache)
		{
		    _bookMarkCache = bookMarkCache;
		    _miningConfiguration = miningConfiguration;
		    _bookmarks = bookmarks;
		    _safespots = safespots;
		    _movement = movement;
		    _mainConfiguration = mainConfiguration;
		    _ship = ship;
		    _meCache = meCache;

		    IsEnabled = true;
            BehaviorManager.BehaviorsToPulse.Add(BotModes.BoostOrca, this);
			ModuleName = "BoostOrca";
		}

		public override void Pulse()
		{
            var methodName = "Pulse";
			LogTrace(methodName);

			if (_mainConfiguration.ActiveBehavior != BotModes.BoostOrca || !ShouldPulse() || _movement.IsMoving) 
				return;

		    if (!_ship.IsInventoryReady) return;

            if (Core.Metatron.Defense.IsFleeing)
            {
                _boostOrcaState = BoostOrcaStates.Idle;
                return;
            }

			SetPulseState();
			ProcessPulseState();
		}

		protected override void SetPulseState()
		{
            var methodName = "SetPulseState";
			LogTrace(methodName);

            if (HasMaxRuntimeExpired())
            {
                _boostOrcaState = BoostOrcaStates.Error;
                return;
            }

			if (_meCache.FleetMembers.Count == 0)
			{
				_boostOrcaState = BoostOrcaStates.WaitingForFleet;
			}
			else if (_boostOrcaState == BoostOrcaStates.WaitingForFleet)
			{
				_boostOrcaState = BoostOrcaStates.Idle;
			}
		}

		protected override void ProcessPulseState()
		{
            var methodName = "ProcessPulseState";
			LogTrace(methodName);

			LogMessage(methodName, LogSeverityTypes.Debug, "State: {0}",
				_boostOrcaState);
			switch (_boostOrcaState)
			{
				case BoostOrcaStates.Idle:
					_boostOrcaState = BoostOrcaStates.GetInPosition;
					goto case BoostOrcaStates.GetInPosition;
				case BoostOrcaStates.GetInPosition:
			        var boostLocationBookmark = _bookMarkCache.FirstBookMarkMatching(_miningConfiguration.BoostOrcaBoostLocationLabel, true);
                    if (boostLocationBookmark == null)
                    {
                        LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not find a bookmark matching the boost orca location label of \"{0}\" in this system.",
                            _miningConfiguration.BoostOrcaBoostLocationLabel);
                        _boostOrcaState = BoostOrcaStates.Error;
                        return;
                    }

					if (_meCache.InStation)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Undocking.");
						_movement.QueueDestination(
							new Destination(DestinationTypes.Undock));
						return;
					}

					//Now I need to get to a safespot in space. Preferrably a tower but I can't know what type it is.
					//If I'm out of warp range, warp to it
                    if (!_bookmarks.IsAtBookmark(boostLocationBookmark))
					{
						_movement.QueueDestination(
                            new Destination(DestinationTypes.BookMark, boostLocationBookmark.Id));
						LogMessage(methodName, LogSeverityTypes.Standard, "Moving to boost location bookmark \"{0}\".",
                            boostLocationBookmark.Label);
					}
					else
					{
						_boostOrcaState = BoostOrcaStates.WaitInPosition;
						goto case BoostOrcaStates.WaitInPosition;
					}
					break;
				case BoostOrcaStates.WaitInPosition:
					//Just chill here and boost for the fleet. Turn on ganglinks as needed.
					if (_ship.GangLinkModules.Count > 0 &&
                        !_ship.GangLinkModules[0].IsActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Activating ganglink modules.");
                        _ship.ActivateModuleList(_ship.GangLinkModules, true);
					}
					if (_ship.DamageControlModules.Count > 0 &&
                        !_ship.DamageControlModules[0].IsActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Activating damage control module.");
					    _ship.ActivateModuleList(_ship.DamageControlModules, false);
					}
					break;
				case BoostOrcaStates.Error:
					//Make sure I'm at a safespot; preferrably in space, station if required.
                    if (!_safespots.IsSafe())
                    {
                        var destination = _safespots.GetSafeSpot();
                        _movement.QueueDestination(destination);
                    }

					IsEnabled = false;
					break;
			}
		}

        protected override void _processCleanupState()
        {
            throw new NotImplementedException();
        }

        protected override void _setCleanupState()
        {
            throw new NotImplementedException();
        }
	}

	public enum BoostOrcaStates
	{
		Idle,
		GetInPosition,
		WaitInPosition,
		Error,
		WaitingForFleet
	}
}
