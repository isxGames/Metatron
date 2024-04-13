using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Metatron.Core.Extensions;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    // ReSharper disable PossibleMultipleEnumeration
    // ReSharper disable ConvertToConstant.Local
    internal sealed class AsteroidBelts : ModuleBase, IAsteroidBelts
    {
        private readonly List<IEntityWrapper> _asteroids = new List<IEntityWrapper>();
        public ReadOnlyCollection<IEntityWrapper> Asteroids { get { return _asteroids.AsReadOnly(); } } 

        private readonly List<IEntityWrapper> _asteroidsInRange = new List<IEntityWrapper>();
        public ReadOnlyCollection<IEntityWrapper> AsteroidsInRange { get { return _asteroidsInRange.AsReadOnly(); } } 

        private readonly List<IEntityWrapper> _asteroidsOutOfRange = new List<IEntityWrapper>();
        public ReadOnlyCollection<IEntityWrapper> AsteroidsOutOfRange { get { return _asteroidsOutOfRange.AsReadOnly(); } }

        private readonly List<CachedBelt> _cachedBelts = new List<CachedBelt>();
        public ReadOnlyCollection<CachedBelt> CachedBelts { get { return _cachedBelts.AsReadOnly(); } }

        private List<CachedBookMarkedBelt> _cachedBookMarkedBelts = new List<CachedBookMarkedBelt>();
        public ReadOnlyCollection<CachedBookMarkedBelt> CachedBookMarkedBelts { get { return _cachedBookMarkedBelts.AsReadOnly(); } }

        private readonly Dictionary<string, int> _asteroidTypeIdsByType = new Dictionary<string, int>(); 
		public Dictionary<string, int> AsteroidTypeIdsByType { get { return _asteroidTypeIdsByType; }}

        private readonly Dictionary<int, string> _asteroidTypesByTypeId = new Dictionary<int, string>();
        public Dictionary<int, string> AsteroidTypesByTypeId { get { return _asteroidTypesByTypeId; } }

        private readonly Dictionary<string, int> _asteroidGroupIdsByGroup = new Dictionary<string, int>(); 
        public Dictionary<string, int> AsteroidGroupIdsByGroup { get { return _asteroidGroupIdsByGroup; }}

        private readonly Dictionary<int, string> _asteroidGroupsByGroupId = new Dictionary<int, string>();
        public Dictionary<int, string> AsteroidGroupsByGroupId { get { return _asteroidGroupsByGroupId; } } 

        public bool AllBeltsEmpty { get; private set; }
        public bool AllBookMarkedBeltsEmpty { get; private set; }
        public int LastSolarSystem { get; private set; }
        public CachedBelt CurrentBelt { get; private set; }
        public CachedBookMarkedBelt CurrentBookMarkedBelt { get; private set; }
    	public bool IsBeltEmpty { get; private set; }

		private bool _isAtBeltSet;
        private bool _isAtBelt;

        private readonly IConfiguration _configuration;
        private readonly IMeCache _meCache;
        private readonly IEntityProvider _entityProvider;
        private readonly IBookMarkCache _bookMarkCache;
        private readonly IShip _ship;

        internal AsteroidBelts(IConfiguration configuration, IMeCache meCache, IEntityProvider entityProvider, IBookMarkCache bookMarkCache, IShip ship)
        {
            _configuration = configuration;
            _meCache = meCache;
            _entityProvider = entityProvider;
            _bookMarkCache = bookMarkCache;
            _ship = ship;

            PulseFrequency = 1;
            ModuleManager.ModulesToPulse.Add(this);
			ModuleName = "AsteroidBelts";

            _asteroidTypeIdsByType.Add("Veldspar", (int)TypeIDs.Veldspar);
            _asteroidTypeIdsByType.Add("Concentrated Veldspar", (int)TypeIDs.Concentrated_Veldspar);
            _asteroidTypeIdsByType.Add("Dense Veldspar", (int)TypeIDs.Dense_Veldspar);
            _asteroidTypeIdsByType.Add("Scordite", (int)TypeIDs.Scordite);
            _asteroidTypeIdsByType.Add("Condensed Scordite", (int)TypeIDs.Condensed_Scordite);
            _asteroidTypeIdsByType.Add("Massive Scordite", (int)TypeIDs.Massive_Scordite);
            _asteroidTypeIdsByType.Add("Pyroxeres", (int)TypeIDs.Pyroxeres);
            _asteroidTypeIdsByType.Add("Solid Pyroxeres", (int)TypeIDs.Solid_Pyroxeres);
            _asteroidTypeIdsByType.Add("Viscous Pyroxeres", (int)TypeIDs.Viscous_Pyroxeres);
            _asteroidTypeIdsByType.Add("Plagioclase", (int)TypeIDs.Plagioclase);
            _asteroidTypeIdsByType.Add("Azure Plagioclase", (int)TypeIDs.Azure_Plagioclase);
            _asteroidTypeIdsByType.Add("Rich Plagioclase", (int)TypeIDs.Rich_Plagioclase);
            _asteroidTypeIdsByType.Add("Omber", (int)TypeIDs.Omber);
            _asteroidTypeIdsByType.Add("Silvery Omber", (int)TypeIDs.Silvery_Omber);
            _asteroidTypeIdsByType.Add("Golden Omber", (int)TypeIDs.Golden_Omber);
            _asteroidTypeIdsByType.Add("Kernite", (int)TypeIDs.Kernite);
            _asteroidTypeIdsByType.Add("Luminous Kernite", (int)TypeIDs.Luminous_Kernite);
            _asteroidTypeIdsByType.Add("Fiery Kernite", (int)TypeIDs.Fiery_Kernite);
            _asteroidTypeIdsByType.Add("Jaspet", (int)TypeIDs.Jaspet);
            _asteroidTypeIdsByType.Add("Pure Jaspet", (int)TypeIDs.Pure_Jaspet);
            _asteroidTypeIdsByType.Add("Pristine Jaspet", (int)TypeIDs.Pristine_Jaspet);
            _asteroidTypeIdsByType.Add("Hemorphite", (int)TypeIDs.Hemorphite);
            _asteroidTypeIdsByType.Add("Vivid Hemorphite", (int)TypeIDs.Vivid_Hemorphite);
            _asteroidTypeIdsByType.Add("Radiant Hemorphite", (int)TypeIDs.Radiant_Hemorphite);
            _asteroidTypeIdsByType.Add("Hedbergite", (int)TypeIDs.Hedbergite);
            _asteroidTypeIdsByType.Add("Vitric Hedbergite", (int)TypeIDs.Vitric_Hedbergite);
            _asteroidTypeIdsByType.Add("Glazed Hedbergite", (int)TypeIDs.Glazed_Hedbergite);
            _asteroidTypeIdsByType.Add("Gneiss", (int)TypeIDs.Gneiss);
            _asteroidTypeIdsByType.Add("Iridescent Gneiss", (int)TypeIDs.Iridescent_Gneiss);
            _asteroidTypeIdsByType.Add("Prismatic Gneiss", (int)TypeIDs.Prismatic_Gneiss);
            _asteroidTypeIdsByType.Add("Dark Ochre", (int)TypeIDs.Dark_Ochre);
            _asteroidTypeIdsByType.Add("Onyx Ochre", (int)TypeIDs.Onyx_Ochre);
            _asteroidTypeIdsByType.Add("Obsidian Ochre", (int)TypeIDs.Obsidian_Ochre);
            _asteroidTypeIdsByType.Add("Crokite", (int)TypeIDs.Crokite);
            _asteroidTypeIdsByType.Add("Sharp Crokite", (int)TypeIDs.Sharp_Crokite);
            _asteroidTypeIdsByType.Add("Crystalline Crokite", (int)TypeIDs.Crystalline_Crokite);
            _asteroidTypeIdsByType.Add("Spodumain", (int)TypeIDs.Spodumain);
            _asteroidTypeIdsByType.Add("Bright Spodumain", (int)TypeIDs.Bright_Spodumain);
            _asteroidTypeIdsByType.Add("Gleaming Spodumain", (int)TypeIDs.Gleaming_Spodumain);
            _asteroidTypeIdsByType.Add("Bistot", (int)TypeIDs.Bistot);
            _asteroidTypeIdsByType.Add("Triclinic Bistot", (int)TypeIDs.Triclinic_Bistot);
            _asteroidTypeIdsByType.Add("Monoclinic Bistot", (int)TypeIDs.Monoclinic_Bistot);
            _asteroidTypeIdsByType.Add("Arkonor", (int)TypeIDs.Arkonor);
            _asteroidTypeIdsByType.Add("Crimson Arkonor", (int)TypeIDs.Crimson_Arkonor);
            _asteroidTypeIdsByType.Add("Prime Arkonor", (int)TypeIDs.Prime_Arkonor);
            _asteroidTypeIdsByType.Add("Mercoxit", (int)TypeIDs.Mercoxit);
            _asteroidTypeIdsByType.Add("Magma Mercoxit", (int)TypeIDs.Magma_Mercoxit);
            _asteroidTypeIdsByType.Add("Vitreous Mercoxit", (int)TypeIDs.Vitreous_Mercoxit);
            _asteroidTypeIdsByType.Add("Banidine", (int)TypeIDs.Banidine);
            _asteroidTypeIdsByType.Add("Augumene", (int)TypeIDs.Augumene);
            _asteroidTypeIdsByType.Add("Mercium", (int)TypeIDs.Mercium);
            _asteroidTypeIdsByType.Add("Lyavite", (int)TypeIDs.Lyavite);
            _asteroidTypeIdsByType.Add("Pithix", (int)TypeIDs.Pithix);
            _asteroidTypeIdsByType.Add("Green Arisite", (int)TypeIDs.Green_Arisite);
            _asteroidTypeIdsByType.Add("Oeryl", (int)TypeIDs.Oeryl);
            _asteroidTypeIdsByType.Add("Geodite", (int)TypeIDs.Geodite);
            _asteroidTypeIdsByType.Add("Polygypsum", (int)TypeIDs.Polygypsum);
            _asteroidTypeIdsByType.Add("Zuthrine", (int)TypeIDs.Zuthrine);
			//Ices
            _asteroidTypeIdsByType.Add("Krystallos", (int)TypeIDs.Krystallos);
            _asteroidTypeIdsByType.Add("Gelidus", (int)TypeIDs.Gelidus);
            _asteroidTypeIdsByType.Add("Dark Glitter", (int)TypeIDs.Dark_Glitter);
            _asteroidTypeIdsByType.Add("Glare Crust", (int)TypeIDs.Glare_Crust);
            _asteroidTypeIdsByType.Add("Enriched Clear Icicle", (int)TypeIDs.Enriched_Clear_Icicle);
            _asteroidTypeIdsByType.Add("Clear Icicle", (int)TypeIDs.Clear_Icicle);
            _asteroidTypeIdsByType.Add("Thick Blue Ice", (int)TypeIDs.Thick_Blue_Ice);
            _asteroidTypeIdsByType.Add("Blue Ice", (int)TypeIDs.Blue_Ice);
            _asteroidTypeIdsByType.Add("Smooth Glacial Mass", (int)TypeIDs.Smooth_Glacial_Mass);
            _asteroidTypeIdsByType.Add("Glacial Mass", (int)TypeIDs.Glacial_Mass);
            _asteroidTypeIdsByType.Add("Pristine White Glaze", (int)TypeIDs.Pristine_White_Glaze);
            _asteroidTypeIdsByType.Add("White Glaze", (int)TypeIDs.White_Glaze);
            _asteroidTypeIdsByType.Add("Azure Ice", (int)TypeIDs.Azure_Ice);
            _asteroidTypeIdsByType.Add("Crystalline Icicle", (int)TypeIDs.Crystalline_Icicle);

            _asteroidGroupIdsByGroup.Add("Veldspar", (int)GroupIDs.Veldspar);
            _asteroidGroupIdsByGroup.Add("Scordite", (int)GroupIDs.Scordite);
            _asteroidGroupIdsByGroup.Add("Pyroxeres", (int)GroupIDs.Pyroxeres);
            _asteroidGroupIdsByGroup.Add("Plagioclase", (int)GroupIDs.Plagioclase);
            _asteroidGroupIdsByGroup.Add("Kernite", (int)GroupIDs.Kernite);
            _asteroidGroupIdsByGroup.Add("Omber", (int)GroupIDs.Omber);
            _asteroidGroupIdsByGroup.Add("Hedbergite", (int)GroupIDs.Hedbergite);
            _asteroidGroupIdsByGroup.Add("Hemorphite", (int)GroupIDs.Hemorphite);
            _asteroidGroupIdsByGroup.Add("Jaspet", (int)GroupIDs.Jaspet);
            _asteroidGroupIdsByGroup.Add("Gneiss", (int)GroupIDs.Gneiss);
            _asteroidGroupIdsByGroup.Add("Ochre", (int)GroupIDs.Dark_Ochre);
            _asteroidGroupIdsByGroup.Add("Spodumain", (int)GroupIDs.Spodumain);
            _asteroidGroupIdsByGroup.Add("Crokite", (int)GroupIDs.Crokite);
            _asteroidGroupIdsByGroup.Add("Bistot", (int)GroupIDs.Bistot);
            _asteroidGroupIdsByGroup.Add("Arkonor", (int)GroupIDs.Arkonor);
            _asteroidGroupIdsByGroup.Add("Mercoxit", (int)GroupIDs.Mercoxit);

            foreach (var asteroidType in _asteroidTypeIdsByType.Keys.ToList())
			{
                _asteroidTypesByTypeId.Add(_asteroidTypeIdsByType[asteroidType], asteroidType);
			}

            foreach (var asteroidGroup in _asteroidGroupIdsByGroup.Keys.ToList())
			{
                _asteroidGroupsByGroupId.Add(_asteroidGroupIdsByGroup[asteroidGroup], asteroidGroup);
			}
        }

        public override void Pulse()
        {
            var methodName = "Pulse";

			LogTrace(methodName);

        	if (!ShouldPulse())
				return;

        	if (_configuration.MainConfig.ActiveBehavior != BotModes.Mining || !_meCache.IsValid ||
        	    !_meCache.InSpace || _meCache.InStation) 
				return;

        	StartPulseProfiling();

            _asteroids.AddRange(
                _entityProvider.EntityWrappers.Where(entity => entity.CategoryID == (int)CategoryIDs.Asteroid)
                );

        	if (_meCache.ToEntity.Mode != (int)Modes.Warp)
        	{
        		//StartMethodProfiling("UpdateAsteroidList");
        		UpdateAsteroidList();
        		//EndMethodProfiling();
        		//StartMethodProfiling("IsBeltEmpty");
        		IsBeltEmpty = DetermineIfBeltIsEmpty();
        		//EndMethodProfiling();
        	}
        	else
        	{
        		IsBeltEmpty = false;
        	}

        	//Rebulid belt lists if we've changed systems
        	if (_meCache.SolarSystemId != LastSolarSystem)
        	{
        		LastSolarSystem = _meCache.SolarSystemId;

        		BuildCachedBeltList(true);
        		BuildCachedBookMarkedBeltList(true);
        	}

        	//If all bookmarked belts were empty...
        	if (AllBookMarkedBeltsEmpty)
        	{
        		//Try to rebuild the list
        		BuildCachedBookMarkedBeltList(false);
        		//try to find an unempty belt
                if (_cachedBookMarkedBelts.Any(cachedBookMarkedBelt => !cachedBookMarkedBelt.IsBeltEmpty))
        		{
        			AllBookMarkedBeltsEmpty = false;
        		}
        	}
        	EndPulseProfiling();
        }

		public override void InFrameCleanup()
		{
            _asteroids.Clear();

			_isAtBeltSet = false;
			_isAtBelt = false;
		}

        public bool IsAtAsteroidBelt()
        {
            var methodName = "IsAtAsteroidBelt";
			LogTrace(methodName);

			if (_isAtBeltSet)
				return _isAtBelt;

			if (!_configuration.MovementConfig.OnlyUseBeltBookmarks)
			{
				foreach (var cachedBelt in _cachedBelts)
				{
					if (!_entityProvider.EntityWrappersById.ContainsKey(cachedBelt.Id)) 
						continue;

					var entity = _entityProvider.EntityWrappersById[cachedBelt.Id];

					if (Distance(entity.X, entity.Y, entity.Z, _meCache.ToEntity.X, _meCache.ToEntity.Y, _meCache.ToEntity.Z) > 300000) 
						continue;

					_isAtBeltSet = true;
					_isAtBelt = true;
					return _isAtBelt;
				}
			}
			else
			{
                foreach (var asteroidBelt in _cachedBookMarkedBelts)
				{
					var distance = Distance(asteroidBelt.X, asteroidBelt.Y, asteroidBelt.Z, _meCache.ToEntity.X, _meCache.ToEntity.Y, _meCache.ToEntity.Z);

					if (distance <= 300000)
					{
						_isAtBeltSet = true;
						_isAtBelt = true;
						return _isAtBelt;
					}

					LogMessage(methodName, LogSeverityTypes.Debug, "Bookmarked belt: \"{0}\", distance: {1} (Me: [{2}, {3}, {4}] Bookmark: [{5}, {6}, {7}])",
						asteroidBelt.BookmarkLabel, distance, _meCache.ToEntity.X, _meCache.ToEntity.Y, _meCache.ToEntity.Z,
						asteroidBelt.X, asteroidBelt.Y, asteroidBelt.Z);
				}

				LogMessage(methodName, LogSeverityTypes.Debug, "No bookmarked belts found.");
			}

			_isAtBeltSet = true;
			_isAtBelt = false;
            return _isAtBelt;
        }

        private void BuildCachedBeltList(bool clearList)
        {
            var methodName = "BuildCachedBeltList";
			LogTrace(methodName, "ClearList: {0}", clearList);

			//Shouldn't always clear
			if (clearList)
			{
				_cachedBelts.Clear();
			}

            var nameFilter = _configuration.MainConfig.ActiveBehavior == BotModes.Mining && _configuration.MiningConfig.IsIceMining ?
                "Ice Field" : "Asteroid Belt";

            var asteroidBelts = _entityProvider.EntityWrappers
                .Where(entity => entity.GroupID == (int)GroupIDs.AsteroidBelt && entity.Name.Contains(nameFilter))
                .ToList();

            if (asteroidBelts.Count == 0)
            {
            	LogMessage(methodName, LogSeverityTypes.Standard, "Found 0 asteroid belts.");
                AllBeltsEmpty = true;
            }

            asteroidBelts = _configuration.MovementConfig.UseRandomBeltOrder ?
                asteroidBelts.RandomizeOrder() : asteroidBelts.OrderBy(x => x.Name).ToList();

			if (_configuration.MovementConfig.UseBeltSubsets)
			{
                asteroidBelts = GetSubsetFromSet(asteroidBelts);
			}

            foreach (var entity in asteroidBelts)
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Found belt {0} ({1})", entity.Name, entity.ID);
                _cachedBelts.Add(new CachedBelt(entity));
            }
        }

		private List<T> GetSubsetFromSet<T>(IList<T> set)
		{
			var methodName = "GetSubsetFromSet";
			LogTrace(methodName);

			var returnValue = new List<T>();
			switch (_configuration.MovementConfig.BeltSubsetMode)
			{
				case BeltSubsetModes.First:
					returnValue = set.Take(_configuration.MovementConfig.NumBeltsInSubset).ToList();
					break;
				case BeltSubsetModes.Middle:
					if (set.Count > _configuration.MovementConfig.NumBeltsInSubset)
					{
						//Get the middle element
						var middleElement = (int)Math.Floor((double)set.Count / 2);
						//Calculate the base of the set I'm taking
						//Calculate the middle of the set I'm taking
						var middleOfSet = (int)Math.Floor((double)_configuration.MovementConfig.NumBeltsInSubset / 2);
						//Start index is middleElement - middleOfset
						var startOfSet = middleElement - middleOfSet;
						//End of set is startOfSet + numBeltsInSubset
						var endOfSet = startOfSet + _configuration.MovementConfig.NumBeltsInSubset;

						for (var index = startOfSet; index < endOfSet; index++)
						{
							returnValue.Add(set[index]);
						}
					}
					break;
				case BeltSubsetModes.Last:
					returnValue = set.Reverse().Take(_configuration.MovementConfig.NumBeltsInSubset).ToList();
					break;
			}
			return returnValue;
		}

        private void BuildCachedBookMarkedBeltList(bool clearList)
        {
			var methodName = "BuildCachedBookMarkedBeltList";
			LogTrace(methodName, "ClearList: {0}", clearList);

			if (clearList)
			{
                _cachedBookMarkedBelts.Clear();
			}

            var prefix = _configuration.MainConfig.ActiveBehavior == BotModes.Mining && _configuration.MiningConfig.IsIceMining ?
                _configuration.MovementConfig.IceBeltBookmarkPrefix : _configuration.MovementConfig.AsteroidBeltBookmarkPrefix;

            var bookMarks = _bookMarkCache.GetBookMarksStartingWith(prefix, true);

            if (bookMarks.Count == 0)
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Found 0 belt bookmarks for prefix {0}.", _configuration.MovementConfig.AsteroidBeltBookmarkPrefix);
                return;
            }

			//prune any bookmarks already cached, remove any no longer existing
            for (var index = 0; index < _cachedBookMarkedBelts.Count; index++)
			{
                var cachedBookMarkedBelt = _cachedBookMarkedBelts[index];
				var beltFound = bookMarks.Any(bookMark => bookMark.Id == cachedBookMarkedBelt.Id);

				if (beltFound) 
					continue;

				LogMessage(methodName, LogSeverityTypes.Debug, "Removing nonexistent bookmark {0} ({1}).",
					cachedBookMarkedBelt.BookmarkLabel, cachedBookMarkedBelt.Id);
                _cachedBookMarkedBelts.RemoveAt(index);
				index--;
			}

			foreach (var bookMark in bookMarks)
			{
                var matchFound = _cachedBookMarkedBelts.Any(bookMarkedBelt => bookMark.Id == bookMarkedBelt.Id);
				if (!matchFound)
				{
                    _cachedBookMarkedBelts.Add(new CachedBookMarkedBelt(bookMark, _configuration.MiningConfig.IsIceMining));
				}
			}

            _cachedBookMarkedBelts = _configuration.MovementConfig.UseRandomBeltOrder ?
                _cachedBookMarkedBelts.RandomizeOrder() : _cachedBookMarkedBelts.OrderBy(x => DistanceTo(x.X, x.Y, x.Z)).ToList();

			if (_configuration.MovementConfig.UseBeltSubsets)
			{
                _cachedBookMarkedBelts = GetSubsetFromSet(_cachedBookMarkedBelts);
			}

            if (_cachedBookMarkedBelts.Count == 0)
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Found 0 bookmarked belts in system.");
            }
        }

        public void ChangeBelts(bool canResetBelts, bool forceMarkEmpty)
        {
            var methodName = "ChangeBelts";
			LogTrace(methodName, "CanResetBelts: {0}, ForceMarkEmpty: {1}",
				canResetBelts, forceMarkEmpty);

            if (_configuration.MovementConfig.OnlyUseBeltBookmarks)
            {
                if (CurrentBookMarkedBelt == null)
                {
					SetCurrentCachedBookMarkedBelt();
					return;
                }

				if (IsBeltEmpty || forceMarkEmpty)
				{
                    var beltBookMark = _bookMarkCache.FirstBookMarkMatching(CurrentBookMarkedBelt.BookmarkLabel, true);
      
					if (!forceMarkEmpty && DistanceTo(beltBookMark.X, beltBookMark.Y, beltBookMark.Z) > (int)Ranges.Warp)
					{
						return;
					}

					LogMessage(methodName, LogSeverityTypes.Debug, "Marking belt {0} empty and changing belt.",
						CurrentBookMarkedBelt.BookmarkLabel);
					CurrentBookMarkedBelt.IsBeltEmpty = true;

					//if I can reset belts and all belts are empty, mark 'em unempty
                    if (canResetBelts && _cachedBookMarkedBelts.Count(x => !x.IsBeltEmpty) == 0)
					{
                        foreach (var belt in _cachedBookMarkedBelts)
						{
							belt.IsBeltEmpty = false;
						}
					}
					SetCurrentCachedBookMarkedBelt();
				}
				else
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Changing belts without marking belt {0} empty.",
						CurrentBookMarkedBelt.BookmarkLabel);
					SetCurrentCachedBookMarkedBelt();
				}
            }
            else
            {
				//If there's currently no belt,just try to set belt.
                if (CurrentBelt == null)
                {
					SetCurrentCachedBelt();
					return;
                }

				//If the belt is detected as empty of asteroids or I'm forced to mark it empty
				if (IsBeltEmpty || forceMarkEmpty)
				{
					var tempBelt = _entityProvider.EntityWrappersById[CurrentBelt.Id];

					//Make sure I'm actually at the belt if not forcing empty
					if (!forceMarkEmpty && tempBelt.Distance > (int)Ranges.Warp)
					{
						return;
					}

					//Set the belt empty
					CurrentBelt.IsBeltEmpty = true;
					LogMessage(methodName, LogSeverityTypes.Debug, "Marking belt {0} empty and changing belt.",
						CurrentBelt.Name);

					//If all belts are empty and I can reset that status, do so
					if (canResetBelts && _cachedBelts.Count(x => !x.IsBeltEmpty) == 0)
					{
						foreach (var belt in _cachedBelts)
						{
							belt.IsBeltEmpty = false;
						}
					}

					SetCurrentCachedBelt();
				}
				else
				{
					LogMessage(methodName, LogSeverityTypes.Debug, "Changing belt without marking belt {0} empty.",
						CurrentBelt.Name);
					SetCurrentCachedBelt();
				}
            }
        }

        private void SetCurrentCachedBookMarkedBelt()
        {
            var methodName = "_setCurrentCachedBMedBelt";
			LogTrace(methodName);

            if (_cachedBookMarkedBelts.Count == 0)
            {
                BuildCachedBookMarkedBeltList(false);
            }

			CachedBookMarkedBelt beltToMakeActive;

			//Try to get the first non-empty belt AFTER the current belt
			//Start by filtering out all empty belts and this belt.
            var nonEmptyBelts = _cachedBookMarkedBelts.Where(x => !x.IsBeltEmpty);

			if (!nonEmptyBelts.Any())
			{
				//If there are no non-empty belts, return.
				LogMessage(methodName, LogSeverityTypes.Standard, "Error: All bookmarked belts are empty.");
				AllBookMarkedBeltsEmpty = true;
			    return;
			}

			//If the current belt is null we just grab the first belt, problem solved.
			if (CurrentBookMarkedBelt == null)
			{
				beltToMakeActive = nonEmptyBelts.First();
			}
			else
			{
				//Now compare the index of the current belt to the count of items to determine if we need to start at
				//the start of the collection or from current position
				var beltList = nonEmptyBelts.ToList();
				var currentIndex = beltList.IndexOf(CurrentBookMarkedBelt);

				//If it was an invalid (-1) result or is the end of the collection, grab the first object
				if (currentIndex == -1 || currentIndex == beltList.Count - 1)
				{
					beltToMakeActive = beltList[0];
				}
				else
				{
					//Otherwise, grab the first object after the current index
					beltToMakeActive = beltList[currentIndex + 1];
				}
			}

			//Update the reference
			CurrentBookMarkedBelt = beltToMakeActive;
			LogMessage(methodName, LogSeverityTypes.Standard, "Making bookmark \"{0}\" the current asteroid belt.",
				CurrentBookMarkedBelt.BookmarkLabel);
        }

        private void SetCurrentCachedBelt()
        {
			var methodName = "SetCurrentCachedBelt";
			LogTrace(methodName);

			if (_cachedBelts.Count == 0)
			{
				BuildCachedBeltList(false);
			}

			CachedBelt beltToMakeActive;

			//Start by filtering out all empty belts.
			var nonEmptyBelts = _cachedBelts.Where(x => !x.IsBeltEmpty);

			//if there are no non-empty belts, return.
			if (!nonEmptyBelts.Any())
			{
				LogMessage(methodName, LogSeverityTypes.Standard, "Error: All belts are empty.");
				AllBeltsEmpty = true;
			    return;
			}

			//If the current belt is null we just grab the first belt, problem solved.
			if (CurrentBelt == null)
			{
				beltToMakeActive = nonEmptyBelts.First();
			}
			else
			{
				//Now compare the index of the current belt to the count of items to determine if we need to start at
				//the start of the collection or from current position
				var beltList = nonEmptyBelts.ToList();
				var currentIndex = beltList.IndexOf(CurrentBelt);

				//If it was an invalid (-1) result or is the end of the collection, grab the first object
				if (currentIndex == -1 || currentIndex == beltList.Count - 1)
				{
					beltToMakeActive = beltList.First();
				}
				else
				{
					//Otherwise, grab the first object after the current index
					beltToMakeActive = beltList[currentIndex + 1];
				}
			}

			//Update the reference
			CurrentBelt = beltToMakeActive;
			LogMessage(methodName, LogSeverityTypes.Standard, "Making entity \"{0}\" the current asteroid belt.", CurrentBelt.Name);
        }

        private bool DetermineIfBeltIsEmpty()
        {
            var methodName = "DetermineIfBeltIsEmpty";
			LogTrace(methodName);

            var cachedAsteroids = _asteroids.Where(asteroid => _asteroidsInRange.Contains(asteroid) || _asteroidsOutOfRange.Contains(asteroid))
                .OrderBy(asteroid => asteroid.Distance)
                .ToList();

            if (cachedAsteroids.Count == 0)
            {
				LogMessage(methodName, LogSeverityTypes.Debug, "No asteroids returned by query.");
                return true;
            }

        	//Ok, need to determine if the roids I'm seeing are "too far".
        	//If I'm using belt bookmarks, and not bookmarking las tposition, and closest roid is above slowboat time, too far.
        	//If I'm not using belt bookmarks, and not bounce warping, and closest roid is aobve slowboat time, too far.
        	if (_configuration.MovementConfig.OnlyUseBeltBookmarks &&
                !_configuration.MovementConfig.UseTempBeltBookmarks &&
        	    cachedAsteroids[0].Distance > _ship.MaxSlowboatDistance)
        	{
				LogMessage(methodName, LogSeverityTypes.Debug, "Using belt bookmarks, not bookmarking last position, and first asteroid out of max slowboat range. {0} ({1}, {2})",
					cachedAsteroids[0].Name, cachedAsteroids[0].ID, cachedAsteroids[0].Distance);
        		return true;
        	}

        	if (!_configuration.MovementConfig.OnlyUseBeltBookmarks &&
        	    !_configuration.MovementConfig.UseBounceWarp &&
        	    cachedAsteroids[0].Distance > _ship.MaxSlowboatDistance)
        	{
				LogMessage(methodName, LogSeverityTypes.Debug, "Not using belt bookmarks, not bounce warping, ,and first asteroid out of max slowboat range. {0} ({1}, {2})",
					cachedAsteroids[0].Name, cachedAsteroids[0].ID, cachedAsteroids[0].Distance);
        		return true;
        	}
        	return false;
        	//if asteroid count == 0
                //return true
            //else
                //if not using temporary mining bookmarks
                //if nearest roid distanc is > maxVelocity * maxSlowboatTime
                    //return true
                //return false;
        }

        private void UpdateAsteroidList()
        {
            var methodName = "UpdateAsteroidList";
			LogTrace(methodName);
            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
            //    String.Format("UpdateAsteroidList: Asteroids in cache - {0}, max distance - {1}",
            //    asteroidCache.CachedEntities.Count, maxDistanceToAsteroid)));

            _asteroidsInRange.Clear();
            _asteroidsOutOfRange.Clear();

            List<string> asteroidTypeNames;
            string asteroidLabel;
            if (_configuration.MiningConfig.IsIceMining)
            {
                asteroidLabel = "ice asteroids";
                asteroidTypeNames = _configuration.MiningConfig.StatusByIce
                    .Where(pair => pair.Value)
                    .Select(pair => pair.Key)
                    .OrderBy(name => _configuration.MiningConfig.PriorityByIceType.IndexOf(name))
                    .ToList();
            }
            else
            {
				//StartMethodProfiling("GetAsteroidTypeNames");
                asteroidLabel = "asteroids";
                asteroidTypeNames = _configuration.MiningConfig.StatusByOre
                    .Where(pair => pair.Value)
                    .Select(pair => pair.Key)
                    .OrderBy(name => _configuration.MiningConfig.PriorityByOreType.IndexOf(name))
                    .ToList();
				//EndMethodProfiling();
            }
			
			//StartMethodProfiling("AddAsteroidsInRange");
			_asteroidsInRange.AddRange(
				from IEntityWrapper ce in _asteroids
                join int id in _asteroidTypesByTypeId.Keys on ce.TypeID equals id
                where asteroidTypeNames.Contains(_asteroidTypesByTypeId[id]) &&
					ce.Distance <= _ship.MaximumMiningRange &&
                    ce.Distance <= _ship.MaxTargetRange
                orderby asteroidTypeNames.IndexOf(_asteroidTypesByTypeId[id]) ascending
				select ce);

			LogMessage(methodName, LogSeverityTypes.Debug, "{0} {1} in range.",
                _asteroidsInRange.Count, asteroidLabel);
			//EndMethodProfiling();

			//StartMethodProfiling("AddAsteroidsOutOfRange");
			_asteroidsOutOfRange.AddRange(
				from IEntityWrapper ce in _asteroids
                join int id in _asteroidTypesByTypeId.Keys on ce.TypeID equals id
                where asteroidTypeNames.Contains(_asteroidTypesByTypeId[id]) &&
					(ce.Distance > _ship.MaximumMiningRange ||
                    ce.Distance > _ship.MaxTargetRange)
                orderby asteroidTypeNames.IndexOf(_asteroidTypesByTypeId[id]) ascending
				select ce);

			LogMessage(methodName, LogSeverityTypes.Debug, "{0} {1} out of range.",
                _asteroidsOutOfRange.Count, asteroidLabel);
			//EndMethodProfiling();
			//EndMethodProfiling();
        }
    }
    // ReSharper restore ConvertToConstant.Local
    // ReSharper restore PossibleMultipleEnumeration
}
