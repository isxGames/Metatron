using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Metatron.Core.CustomEventArgs;
using Metatron.Core.Interfaces;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace Metatron.Core
{
    internal class Statistics : ModuleBase, IStatistics
    {
		public event EventHandler<PairEventArgs<string, int>> OnAddIceOreMined;
		public event EventHandler<TimeSpanEventArgs> OnDropoff;
		public event EventHandler<PairEventArgs<string, int>> OnCrystalsUsed;
		public event EventHandler<WalletStatisticsUpdatedEventArgs> WalletStatisticsUpdated;

        private readonly Dictionary<string, int> _minedItemsMovedByItemName = new Dictionary<string, int>(); 
		public Dictionary<string, int> MinedItemsMovedByItemName { get { return _minedItemsMovedByItemName; }}

        private readonly Dictionary<string, int> _quantitiesUsedByItemName = new Dictionary<string, int>();
        public Dictionary<string, int> QuantityChargesUsedByName { get { return _quantitiesUsedByItemName; } }

		private int _numberMiningCargoFulls;
		private TimeSpan _totalTimeMining;
		private double _startingWalletBalance, _lastAverageIskPerHour, _lastIskDeltaThisSession;

		public Statistics()
		{
			ModuleManager.ModulesToPulse.Add(this);
			IsEnabled = true;
			ModuleName = "Statistics";
		}

		public override void Pulse()
		{
			if (!ShouldPulse())
				return;

			TrackWalletStats();
		}

        public override bool OutOfFrameCleanup()
        {
            if (!IsCleanedUpOutOfFrame)
            {
                LogStatisticalInfo();
                IsCleanedUpOutOfFrame = true;
            }
            return IsCleanedUpOutOfFrame;
        }

		private void TrackWalletStats()
		{
			if (Metatron.MeCache.WalletBalance == double.MinValue) return;

			if (_startingWalletBalance == 0)
				_startingWalletBalance = Metatron.MeCache.WalletBalance;

			_lastIskDeltaThisSession = Metatron.MeCache.WalletBalance - _startingWalletBalance;

			_lastAverageIskPerHour = _lastIskDeltaThisSession / Metatron.Instance.RunTime.Elapsed.TotalHours;

			var eventArgs = new WalletStatisticsUpdatedEventArgs(_lastAverageIskPerHour, _lastIskDeltaThisSession);

			if (WalletStatisticsUpdated != null)
				WalletStatisticsUpdated(this, eventArgs);
		}

		private void LogStatisticalInfo()
		{
			if (string.IsNullOrEmpty(Metatron.MeCache.Name)) 
				return;

			var statisticsFilePath = String.Format("{0}\\Metatron\\logs\\{1} Statistics.txt",
			                                          Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), Metatron.MeCache.Name);

			using (TextWriter tw = new StreamWriter(statisticsFilePath, true))
			{
				var separatorLine = new StringBuilder();
				for (var idx = 0; idx < 72; idx++)
				{
					separatorLine.Append("=");
				}

				tw.WriteLine(separatorLine.ToString());
				tw.WriteLine("Session statistical information for {0} {1} up to {2} {3}",
                    Metatron.TimeOfStart.ToShortDateString(), Metatron.TimeOfStart.ToShortTimeString(),
                    DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString());

				tw.WriteLine("Ice and Ore Mined:");
				var iceOreTypes = MinedItemsMovedByItemName.Keys.ToList();
				foreach (var oreType in iceOreTypes)
				{
					tw.WriteLine("\t{0}{1}", oreType.PadRight(30, ' '), MinedItemsMovedByItemName[oreType]);
				}

				tw.WriteLine("Mining Crystals Consumed:");
				var crystalTypes = QuantityChargesUsedByName.Keys.ToList();
				for (var idx = 0; idx < QuantityChargesUsedByName.Count; idx++)
				{
					tw.WriteLine("\t{0}{1}", crystalTypes[idx].PadRight(30, ' '), QuantityChargesUsedByName[crystalTypes[idx]]);
				}

				tw.WriteLine("Mining 'drop off' occurences: {0}", _numberMiningCargoFulls);

				long num = 0;
				if (_numberMiningCargoFulls > 0)
				{
					num = _totalTimeMining.Ticks / _numberMiningCargoFulls;
				}
				tw.WriteLine("Average time per 'drop off' occurence: {0}", TimeSpan.FromTicks(num));

				tw.WriteLine("Wallet balance change this session: {0}", _lastIskDeltaThisSession.ToString("N"));
				tw.WriteLine("Average ISK per Hour this session:  {0}", _lastAverageIskPerHour.ToString("N"));

				tw.WriteLine(separatorLine.ToString());
			}
		}

		public void TrackIceOrOreMined(string type, int quantity)
		{
			if (!MinedItemsMovedByItemName.ContainsKey(type))
			{
				MinedItemsMovedByItemName.Add(type, quantity);
			}
			else
			{
				MinedItemsMovedByItemName[type] += quantity;
			}

			if (OnAddIceOreMined != null)
			{
				OnAddIceOreMined(this, new PairEventArgs<string, int>(type, quantity));
			}
		}

		public void TrackDropoff(TimeSpan timeTaken)
		{
			_numberMiningCargoFulls++; 
			_totalTimeMining += timeTaken;

			if (OnDropoff != null)
			{
				TimeSpan averageTime = TimeSpan.FromTicks(
					_totalTimeMining.Ticks / _numberMiningCargoFulls);
				OnDropoff(this, new TimeSpanEventArgs(
					averageTime));
			}
		}

		public void TrackCrystalUsed(string type, int quantity)
		{
			if (!QuantityChargesUsedByName.ContainsKey(type))
			{
				QuantityChargesUsedByName.Add(type, quantity);
			}
			else
			{
				QuantityChargesUsedByName[type] += quantity;
			}

			if (OnCrystalsUsed != null)
			{
				OnCrystalsUsed(this, new PairEventArgs<string, int>(type, quantity));
			}
		}
	}
}
// ReSharper restore CompareOfFloatsByEqualityOperator