using System;
using Metatron.Core;
using Metatron.ActionModules;
using Metatron.Core.Interfaces;

namespace Metatron.BehaviorModules
{
	public sealed class BoostCanOrca : BehaviorBase
	{
		private BoostCanOrcaStates _boostCanOrcaState = BoostCanOrcaStates.Idle;

		public BoostCanOrca()
		{
			IsEnabled = true;
			ModuleName = "BoostCanOrca";
			BehaviorManager.BehaviorsToPulse.Add(BotModes.BoostCanOrca, this);
		}

		public override void Pulse()
		{
            var methodName = "Pulse";
			LogTrace(methodName);

			if (!ShouldPulse() || Core.Metatron.Config.MainConfig.ActiveBehavior != BotModes.BoostCanOrca ||
			    Core.Metatron.Defense.IsFleeing || Core.Metatron.Movement.IsMoving) 
				return;

		    if (!Core.Metatron.Ship.IsInventoryReady) return;

			SetPulseState();
			ProcessPulseState();
		}

		protected override void SetPulseState()
		{
            var methodName = "SetPulseState";
			LogTrace(methodName);

			if (Core.Metatron.MeCache.FleetMembers.Count == 0)
			{
				_boostCanOrcaState = BoostCanOrcaStates.WaitingForFleet;
			}
			else if (_boostCanOrcaState == BoostCanOrcaStates.WaitingForFleet)
			{
				_boostCanOrcaState = BoostCanOrcaStates.Idle;
			}
		}

		protected override void ProcessPulseState()
		{
            var methodName = "ProcessPulseState";
			LogTrace(methodName);

			switch (_boostCanOrcaState)
			{
				case BoostCanOrcaStates.Idle:
					_boostCanOrcaState = BoostCanOrcaStates.GetInPosition;
					goto case BoostCanOrcaStates.GetInPosition;
				case BoostCanOrcaStates.GetInPosition:
					if (Core.Metatron.MeCache.InStation)
					{
						LogMessage(methodName, LogSeverityTypes.Debug, "Undocking.");
						Core.Metatron.Movement.QueueDestination(new Destination(DestinationTypes.Undock));
						return;
					}

                    //This is never getting to the WaitInPosition state, never setting it.
                    //Should be using ChangeBelt. In fact, should make SetCurrent* private.
                    //As such, this needs refactored.

					//Our only option here is to use belt bookmarks. Specifically, an ice belt.
					if (Core.Metatron.Config.MovementConfig.OnlyUseBeltBookmarks &&
                        Core.Metatron.Config.MiningConfig.IsIceMining)
					{
                        if (Core.Metatron.AsteroidBelts.CurrentBookMarkedBelt == null)
                        {
                            Core.Metatron.AsteroidBelts.ChangeBelts(false, false);
                        }
						//Also need to be an ice belt.

                        if (Core.Metatron.AsteroidBelts.CurrentBookMarkedBelt != null)
                        {
                            if (DistanceTo(Core.Metatron.AsteroidBelts.CurrentBookMarkedBelt.X,
                                Core.Metatron.AsteroidBelts.CurrentBookMarkedBelt.Y,
                                Core.Metatron.AsteroidBelts.CurrentBookMarkedBelt.Z) >= (int)Ranges.Warp)
                            {
								LogMessage(methodName, LogSeverityTypes.Standard, "Moving to bookmark {0}",
									Core.Metatron.AsteroidBelts.CurrentBookMarkedBelt.BookmarkLabel);
                                Core.Metatron.Movement.QueueDestination(
									new Destination(DestinationTypes.BookMark, Core.Metatron.AsteroidBelts.CurrentBookMarkedBelt.Id) { Distance = 3500 });
                            }
                            else
                            {
								LogMessage(methodName, LogSeverityTypes.Standard, "In position at bookmark {0}, waiting in position.",
									Core.Metatron.AsteroidBelts.CurrentBookMarkedBelt.BookmarkLabel);
                                _boostCanOrcaState = BoostCanOrcaStates.WaitInPosition;
                                goto case BoostCanOrcaStates.WaitInPosition;
                            }
                        }
                        else
                        {
                        	LogMessage(methodName, LogSeverityTypes.Standard,
                        	           "Could not set a bookmarked belt, ensure you have Ice Mining checked and have a valid ice belt; error.");
                            _boostCanOrcaState = BoostCanOrcaStates.Error;
                            goto case BoostCanOrcaStates.Error;
                        }
					}
					else
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Not using belt bookmarks or not ice mining; error.");
						_boostCanOrcaState = BoostCanOrcaStates.Error;
						goto case BoostCanOrcaStates.Error;
					}
					break;
				case BoostCanOrcaStates.WaitInPosition:
					//All I need to do here is make sure my ganglinks are on. This doesn't particularly belong in
					//NonOffensive since it's not a targeted action.
					if (Core.Metatron.Ship.GangLinkModules.Count > 0 &&
                        !Core.Metatron.Ship.GangLinkModules[0].IsActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Activating ganglink modules.");
                        Core.Metatron.Ship.ActivateModuleList(Core.Metatron.Ship.GangLinkModules, true);
					}
					if (Core.Metatron.Ship.DamageControlModules.Count > 0 &&
                        !Core.Metatron.Ship.DamageControlModules[0].IsActive)
					{
						LogMessage(methodName, LogSeverityTypes.Standard, "Activating damage control module.");
					    Core.Metatron.Ship.ActivateModuleList(Core.Metatron.Ship.DamageControlModules, false);
					}
					break;
				case BoostCanOrcaStates.Error:
					//Get safe.
					
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

	public enum BoostCanOrcaStates
	{
		Idle,
		GetInPosition,
		WaitInPosition,
		Error,
		WaitingForFleet
	}
}