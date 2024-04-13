using System;
using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE;
using EVE.ISXEVE.Interfaces;
using Metatron.Core.Extensions;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    internal sealed class JettisonContainer : CargoContainer, IJettisonContainer
    {
        public Int64 CurrentContainerId { get; private set; }
        public override IEntityWrapper CurrentContainer
        {
            get {
                return Metatron.EntityProvider.EntityWrappersById.ContainsKey(CurrentContainerId) ?
                           Metatron.EntityProvider.EntityWrappersById[CurrentContainerId] : null;
            }
        }

        private string _canName = string.Empty, _fullCanName = string.Empty;
        private DateTime _lastCanLaunch;
        private List<Int64> _preJettisonCanIDs;

        public JettisonContainer(IEveWindowProvider eveWindowProvider)
            : base(eveWindowProvider)
        {
            CurrentContainerId = -1;
            ModuleName = "JetCan";
        }

        private List<Int64> GetNearbyJetCans()
        {
            //var result = from entity in Metatron.EntityPopulator.EntityWrappers
            //             where entity.Distance <= (int) Ranges.LootActivate &&
            //                   entity.GroupID == (int) GroupIDs.CargoContainer
            //             select entity.ID;

            var result = Metatron.EntityProvider.EntityWrappers
                                   .Where(entity => entity.GroupID == (int) GroupIDs.CargoContainer &&
                                                    entity.Distance <= (int) Ranges.LootActivate)
                                   .Select(entity => entity.ID);

            return result.ToList();
        }

        public void CreateJetCan(IItem itemToJettison)
        {
            var methodName = "CreateJetCan";
            LogTrace(methodName, "Item: {0}", itemToJettison.Name);

            //If it's been long enough since we last jettisoned a can, go ahead.
            if (Metatron.TimeOfPulse.CompareTo(_lastCanLaunch) < 0) 
                return;

            _preJettisonCanIDs = GetNearbyJetCans();

            //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
            //    "CreateJetCan", String.Format("Cans in range before jettison - {0}", _preJettisonCanIDs.Count)));
            itemToJettison.Jettison();
            //Can jettison a can once every 3 miutes
            _lastCanLaunch = DateTime.Now.AddSeconds(180);
            LogMessage(methodName, LogSeverityTypes.Standard, "Jettisoning item {0}.",
                       itemToJettison.Name);
        }

        public void SetActiveCan()
        {
            var methodName = "SetActiveCan";
            LogTrace(methodName);

            //if I already have an active can, return
            if (CurrentContainer != null)
                return;

            if (_preJettisonCanIDs == null)
                _preJettisonCanIDs = GetNearbyJetCans();

            //Declare these and do casting before the linq expression to try and shave off some execution time
            int groupID = (int)GroupIDs.CargoContainer, range = (int)Ranges.LootActivate;
            var currentCans = (from IEntityWrapper entity in Metatron.EntityProvider.EntityWrappers
                               where entity.GroupID == groupID && entity.Distance <= range && 
                                     !_preJettisonCanIDs.Contains(entity.ID) && !entity.Name.Contains("full", StringComparison.InvariantCultureIgnoreCase)
                               select entity).ToList();

            foreach (var e in currentCans)
            {
                var toEntity = e.ToEntity;
                //Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                //    "SetActiveCan", String.Format("Can - {0} ({1}), Owner - {2}, OwnerID - {3}, Owner.CharID - {4}",
                //    e.Name, e.ID, toEntity.Owner.Name, toEntity.OwnerID, toEntity.Owner.CharID)));
                //If we own the can and we didn't have it pre-jettison, it's the one we want
                if (toEntity.Owner.CharID != Metatron.MeCache.CharId) 
                    continue;

                LogMessage(methodName, LogSeverityTypes.Debug, "Setting active jetcan to {0} ({1})",
                           e.Name, e.ID);
                CurrentContainerId = e.ID;

                //Get the not-full and full names for this can
                var canNames = GetFormattedCanNames();
                _canName = canNames[0];
                _fullCanName = canNames[1];
                break;
            }
        }

        public List<string> GetFormattedCanNames()
        {
            var methodName = "GetFormattedCanNames";
            LogTrace(methodName);

            var ret = new List<string>();

            var tempNormalFormat = Metatron.Config.CargoConfig.CanNameFormat;
            var tempFullFormat = Metatron.Config.CargoConfig.CanNameFormat;

            if (tempNormalFormat.Contains("CORP"))
            {
                if (Metatron.MeCache.CorporationTicker != string.Empty)
                {
                    tempNormalFormat = tempNormalFormat.Replace("CORP", Metatron.MeCache.CorporationTicker);
                    tempFullFormat = tempFullFormat.Replace("CORP", Metatron.MeCache.CorporationTicker);
                }
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Jetcan Name Format did not contain \'CORP\' tag. Not using corp ticker.");
            }

            if (tempNormalFormat.Contains("HH"))
            {
                tempNormalFormat = tempNormalFormat.Replace("HH", Metatron.MeCache.GameHour.ToString("00"));
                tempFullFormat = tempFullFormat.Replace("HH", Metatron.MeCache.GameHour.ToString("00"));
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Jetcan Name Format did not contain \'HH\' tag. Not using game hour.");
            }

            if (tempNormalFormat.Contains("MM"))
            {
                tempNormalFormat = tempNormalFormat.Replace("MM", Metatron.MeCache.GameMinute.ToString("00"));
                tempFullFormat = tempFullFormat.Replace("MM", Metatron.MeCache.GameMinute.ToString("00"));
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Jetcan Name Format did not contain \'MM\' tag. Not using game minute.");
            }

            if (tempNormalFormat.Contains("FULL"))
            {
                tempNormalFormat = tempNormalFormat.Replace("FULL", string.Empty);
            }
            else
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Jetcan Name Format did not contain \'FULL\' tag. Not displaying full status.");
            }

            ret.Add(tempNormalFormat);
            ret.Add(tempFullFormat);
            return ret;
        }

        public void RenameActiveCan(bool isFull)
        {
            var methodName = "RenameActiveCan";
            LogTrace(methodName, "IsFull: {0}", isFull);

            if (isFull)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Setting current container's name to {0}",
                           _fullCanName);

                CurrentContainer.SetName(_fullCanName);
                //Add this can to pre-jettissoned to exclude it from being made active if the user
                // for some reason isn't marking it full. Bad user.
                if (!_preJettisonCanIDs.Contains(CurrentContainerId))
                {
                    _preJettisonCanIDs.Add(CurrentContainerId);
                }
            }
            else if (CurrentContainer.Name == "Cargo Container")
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Setting current container's name to {0}",
                           _canName);
                CurrentContainer.SetName(_canName);
            }
        }

        public void RenameActiveCan()
        {
            RenameActiveCan(false);
        }

        public void MarkActiveCanFull()
        {
            RenameActiveCan(true);
            CurrentContainerId = -1;
        }

        public bool IsActiveCanFull()
        {
            return UsedCapacity/Capacity > 0.9;
        }

        public bool IsActiveContainerHalfFull()
        {
            return UsedCapacity/Capacity >= 0.5;
        }
    }
}