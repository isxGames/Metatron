using System.Linq;
using Metatron.ActionModules;
using Metatron.Core;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;

namespace Metatron.BehaviorModules.PartialBehaviors
{
	public class MoveToDropOffLocationPartialBehavior : PartialBehaviorBase
	{
// ReSharper disable ConvertToConstant.Local
	    private readonly int MinimumDistanceToArray = 25000;
// ReSharper restore ConvertToConstant.Local

	    private readonly IMovement _movement;
	    private readonly IEntityProvider _entityProvider;
	    private readonly ICargoConfiguration _cargoConfiguration;
	    private readonly IBookMarkCache _bookMarkCache;
	    private readonly IBookmarks _bookmarks;
	    private readonly IMovementConfiguration _movementConfiguration;
	    private readonly IMeCache _meCache;
	    private readonly MathUtility _mathUtility;
	    private readonly IIsxeveProvider _isxeveProvider;

	    public MoveToDropOffLocationPartialBehavior(IMovement movement, IEntityProvider entityProvider, ICargoConfiguration cargoConfiguration, IBookMarkCache bookMarkCache, IBookmarks bookmarks, 
            IMovementConfiguration movementConfiguration, IMeCache meCache, MathUtility mathUtility, IIsxeveProvider isxeveProvider)
	    {
	        _movement = movement;
	        _entityProvider = entityProvider;
	        _cargoConfiguration = cargoConfiguration;
	        _bookMarkCache = bookMarkCache;
	        _bookmarks = bookmarks;
	        _movementConfiguration = movementConfiguration;
	        _meCache = meCache;
	        _mathUtility = mathUtility;
	        _isxeveProvider = isxeveProvider;

	        ModuleName = "MoveToDropOffLocationPartialBehavior";
	    }

	    public override BehaviorExecutionResults Execute()
		{
			var methodName = "Execute";
			LogTrace(methodName);

			if (_movement.IsMoving)
				return BehaviorExecutionResults.Incomplete;

			if (!IsAtDropOffLocation())
				return MoveToDropOffLocation();

			return BehaviorExecutionResults.Complete;
		}

		public BehaviorExecutionResults MoveToDropOffLocation()
		{
			var methodName = "MoveToDropOffLocation";
			LogTrace(methodName);

			switch (_cargoConfiguration.DropoffLocation.LocationType)
			{
				case LocationTypes.StationCorpHangar:
					goto case LocationTypes.Station;
				case LocationTypes.Station:
					return MoveToDropOffStation();
				case LocationTypes.CorpHangarArray:
					return MoveToDropOffCorporateHangarArray();
				case LocationTypes.ShipBay:
					return MoveToDropOffShip();
			}

			return BehaviorExecutionResults.Error; // This should pretty much never happen.
		}

		private BehaviorExecutionResults MoveToDropOffShip()
		{
			var methodName = "MoveToDropOffShip";
			LogTrace(methodName);

			if (!_entityProvider.EntityWrappersById.ContainsKey(_cargoConfiguration.DropoffLocation.EntityID))
			{
				LogMessage(methodName, LogSeverityTypes.Critical, "Error: Could not find dropoff ship entity with ID {0}.",
					_cargoConfiguration.DropoffLocation.EntityID);
				return BehaviorExecutionResults.Error;
			}

			var shipEntity = _entityProvider.EntityWrappersById[_cargoConfiguration.DropoffLocation.EntityID];

			if (shipEntity.Distance <= (int)Ranges.LootActivate) return BehaviorExecutionResults.Incomplete;

			var shipDestination = new Destination(DestinationTypes.Entity, shipEntity.ID)
			                      	{
			                      		Distance = (int)Ranges.LootActivate
			                      	};
			_movement.QueueDestination(shipDestination);
			return BehaviorExecutionResults.Incomplete;
		}

		private BehaviorExecutionResults MoveToDropOffCorporateHangarArray()
		{
			var methodName = "MoveToDropOffCorporateHangarArray";
			LogTrace(methodName);

			//First queue the CHA bookmark
			var cachedBookMark = _bookMarkCache.FirstBookMarkStartingWith(
				_cargoConfiguration.DropoffLocation.BookmarkLabel, true);

			if (cachedBookMark == null)
			{
				LogMessage(methodName, LogSeverityTypes.Critical, "Error: Could not find the dropoff bookmark with label \"{0}\".",
					_cargoConfiguration.DropoffLocation.BookmarkLabel);
				return BehaviorExecutionResults.Error;
			}

			//If I'm not on grid with the bookmark, get there
			if (!_bookmarks.IsAtBookmark(cachedBookMark))
			{
				var corporateHangarArrayBookmarkDestination = new Destination(DestinationTypes.BookMark, cachedBookMark.Id) { Distance = MinimumDistanceToArray };
				_movement.QueueDestination(corporateHangarArrayBookmarkDestination);
                LogMessage(methodName, LogSeverityTypes.Standard, "Moving to dropoff location bookmark with label \"{0}\".", cachedBookMark.Label);
				return BehaviorExecutionResults.Incomplete;
			}

			if (!_entityProvider.EntityWrappersById.ContainsKey(_cargoConfiguration.DropoffLocation.EntityID))
			{
				LogMessage(methodName, LogSeverityTypes.Critical, "Error: Could not find the dropoff entity with ID {0}.",
					_cargoConfiguration.DropoffLocation.EntityID);
				return BehaviorExecutionResults.Error;
			}

			//find the entity for the tower this CHA is at and move to within shield range of it
            var towerEntity = _entityProvider.EntityWrappers.FirstOrDefault(e => e.GroupID == (int)GroupIDs.ControlTower);
            if (towerEntity == null)
            {
                _logging.LogMessage(ModuleName, methodName, LogSeverityTypes.Standard, "Error: Could not find a control tower at dropoff bookmark \"{0}\".", _cargoConfiguration.DropoffLocation.BookmarkLabel);
                return BehaviorExecutionResults.Error;
            }

		    var itemInfo = _isxeveProvider.Eve.ItemInfo(towerEntity.TypeID);

		    var destination = new Destination(DestinationTypes.Entity, towerEntity.ID)
		        {
		            Distance = itemInfo.ShieldRadius
		        };
		    _movement.QueueDestination(destination);
			LogMessage(methodName, LogSeverityTypes.Standard, "Moving to within {0}m of entity \"{1}\" ({2}, {3}).",
                itemInfo.ShieldRadius, towerEntity.Name, towerEntity.ID, towerEntity.Distance);
			return BehaviorExecutionResults.Incomplete;
		}

		private BehaviorExecutionResults MoveToDropOffStation()
		{
			var methodName = "MoveToDropOffStation";
			LogTrace(methodName);

			if (_cargoConfiguration.DropoffLocation.BookmarkLabel == string.Empty)
			{
				if (_movementConfiguration.HomeStation == string.Empty)
				{
					LogMessage(methodName, LogSeverityTypes.Critical,
						"Error: TrackDropoff Location > Label not set and no home station was found. Set a TrackDropoff Location or start Metatron from inside a station.");
					return BehaviorExecutionResults.Error;
				}

				var dropoffStationEntity = _entityProvider.EntityWrappers
					.FirstOrDefault(entity => entity.Name == _movementConfiguration.HomeStation);

				if (dropoffStationEntity == null)
				{
					LogMessage(methodName, LogSeverityTypes.Critical, "Error: Could not find entity with home station name \"{0}\".",
						_movementConfiguration.HomeStation);
					return BehaviorExecutionResults.Error;
				}

				var dropoffStationDestination = new Destination(DestinationTypes.Entity, dropoffStationEntity.ID)
				                                	{
				                                		Distance = (int) Ranges.Dock, Dock = true
				                                	};
				_movement.QueueDestination(dropoffStationDestination);
				return BehaviorExecutionResults.Incomplete;
			}
			else
			{
				var dropoffStationBookmark = _bookMarkCache.FirstBookMarkStartingWith(
					_cargoConfiguration.DropoffLocation.BookmarkLabel, false);

				if (dropoffStationBookmark == null)
				{
					LogMessage(methodName, LogSeverityTypes.Standard, "Error; Could not find a bookmark with dropoff bookmark label \"{0}\".",
					           _cargoConfiguration.DropoffLocation.BookmarkLabel);
					return BehaviorExecutionResults.Error;
				}

				var dropoffStationDestination = new Destination(DestinationTypes.BookMark, dropoffStationBookmark.Id)
				                                	{
				                                		Dock = true, Distance = (int)Ranges.Dock
				                                	};
				_movement.QueueDestination(dropoffStationDestination);
				return BehaviorExecutionResults.Incomplete;
			}
		}

		public bool IsAtDropOffLocation()
		{
			var methodName = "IsAtDropOffLocation";
			LogTrace(methodName);

			switch (_cargoConfiguration.DropoffLocation.LocationType)
			{
				case LocationTypes.StationCorpHangar:
					goto case LocationTypes.Station;
				case LocationTypes.Station:
					//Am I in the dropoff station?
					if (_meCache.InSpace)
						return false;

					//If the TrackDropoff Location is set, use it.
					if (!string.IsNullOrEmpty(_cargoConfiguration.DropoffLocation.BookmarkLabel))
					{
						var bookMark = _bookMarkCache.FirstBookMarkStartingWith(
							_cargoConfiguration.DropoffLocation.BookmarkLabel, false);

						return _meCache.StationId == bookMark.ItemId;
					}

					//Ok, no dropoff location. Check for the home station.
                    return _movementConfiguration.HomeStation == _meCache.Me.Station.Name;
				case LocationTypes.CorpHangarArray:
					if (_meCache.InStation)
						return false;

                    var cachedBookMark = _bookMarkCache.FirstBookMarkStartingWith(_cargoConfiguration.DropoffLocation.BookmarkLabel, true);
			        if (cachedBookMark == null)
			        {
				        LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not find a bookmark with label starting with \"{0}\" in the current system.",
					        _cargoConfiguration.DropoffLocation.BookmarkLabel);
				        return false;
			        }

                    //If I'm not at the dropoff bookmark, I'm can't be at the dropoff
			        var distanceToBookMark = _mathUtility.Distance(cachedBookMark.X, cachedBookMark.Y, cachedBookMark.Z, _meCache.ToEntity.X, _meCache.ToEntity.Y, _meCache.ToEntity.Z);
			        if (distanceToBookMark > (int)Ranges.Warp)
                        return false;

			        var towerEntity = _entityProvider.EntityWrappers.FirstOrDefault(e => e.GroupID == (int) GroupIDs.ControlTower);
                    if (towerEntity == null)
                    {
                        _logging.LogMessage(ModuleName, methodName, LogSeverityTypes.Standard, "Error: Could not find a control tower at dropoff bookmark \"{0}\".", _cargoConfiguration.DropoffLocation.BookmarkLabel);
                        return false;
                    }

			        var itemInfo = _isxeveProvider.Eve.ItemInfo(towerEntity.TypeID);
			        return towerEntity.Distance <= itemInfo.ShieldRadius;
				case LocationTypes.ShipBay:
					goto case LocationTypes.CorpHangarArray;
			}

			return false;
		}
	}
}
