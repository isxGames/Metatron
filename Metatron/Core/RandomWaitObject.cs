using System;
using System.Collections.Generic;
using System.Linq;
using Metatron.Core.Config;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    internal class RandomWaitObject : ModuleBase, IRandomWaitObject
    {
        private readonly Dictionary<KeyValuePair<int, int>, double> _waitChancesByDelay = new Dictionary<KeyValuePair<int, int>, double>();
        private bool _timerSet;
		private DateTime _timer = DateTime.Now;
        private readonly Random _random;

        private readonly IMaxRuntimeConfiguration _maxRuntimeConfiguration;

        public RandomWaitObject(string parentName)
        {
            _maxRuntimeConfiguration = Metatron.Config.MaxRuntimeConfig;

			IsEnabled = false;
			ModuleName = String.Format("{0} RWO", parentName);
            _random = new Random(DateTime.Now.Millisecond);
        }

        public RandomWaitObject(string parentName, ILogging logging, IMaxRuntimeConfiguration maxRuntimeConfiguration)
            : base(logging)
        {
            _maxRuntimeConfiguration = maxRuntimeConfiguration;

            IsEnabled = false;
            ModuleName = String.Format("{0} RWO", parentName);
            _random = new Random(DateTime.Now.Millisecond);
        }

        public void AddWait(KeyValuePair<int, int> rangeToWait, double chance)
        {
            if (!_waitChancesByDelay.ContainsValue(chance))
            {
                _waitChancesByDelay.Add(rangeToWait, chance);
            }
        }

        public bool ShouldWait()
        {
            var methodName = "ShouldWait";
			LogTrace(methodName);

            if (!_maxRuntimeConfiguration.UseRandomWaits)
                return false;

            if (!_timerSet)
            {
                //If we haven't already set the timer, set it
                var randomNumber = _random.Next(10000);
                var randomPercent = (double)randomNumber / 100;

                foreach (var rangeToWait in
                	_waitChancesByDelay.Keys.Where(rangeToWait => randomPercent < _waitChancesByDelay[rangeToWait]))
                {
                	//calculate the delta of the range
                    var delta = Math.Abs(rangeToWait.Value - rangeToWait.Key);

                	//Our 'seconds to wait' will be the Math.Rand of the delta, so it'll be a random value between
                	//0 and the delta. Take that value and add the smaller # of the range to it so we effectively
                	//have a random # between the small # of the range and the large # of the range.
                    var secondsToWait = _random.Next(delta) + rangeToWait.Key;

                	//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
                	//    "ShouldWait", String.Format("{0}: Waiting {1} seconds. ({2}% chance)",
                	//    ObjectName.PadRight(25), secondsToWait, _waitTimes_Chances[secondsToWait])));
                	//If we'r waiting 5 minutes or longer let the user know so they don't think Metatron froze
                	if (secondsToWait >= 300)
                	{
                		Metatron.Alerts.LongRandomWait(secondsToWait);
						LogMessage(methodName, LogSeverityTypes.Standard, "Warning: Beginning a {0}-second (long) random wait. SB has NOT frozen.",
							secondsToWait);
                	}

                	_timer = DateTime.Now.AddSeconds(secondsToWait);
                	_timerSet = true;
                	return true;
                }
                //Core.Metatron.Logging.LogMessage(_parent, new LogEventArgs(LogSeverityTypes.Debug,
                //    "ShouldWait", String.Format("{0}: Not waiting.",
                //    _parent.PadRight(15))));
                return false;
            }

        	if (DateTime.Now.CompareTo(_timer) >= 0)
        	{
        		//Core.Metatron.Logging.LogMessage(ObjectName, new LogEventArgs(LogSeverityTypes.Debug,
        		//    "ShouldWait", string.Format("{0}: Done waiting!",
        		//    ObjectName.PadRight(25))));
        		_timerSet = false;
        		return false;
        	}
        	return true;
        }
    }
}
