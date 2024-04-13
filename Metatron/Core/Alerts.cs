using System;
using System.Speech.Synthesis;
using Metatron.Core.Interfaces;

namespace Metatron.Core
{
    /// <summary>
    /// Provide easy access to speech synthesizing for Metatron alerts.
    /// </summary>
    public sealed class Alerts : IDisposable, IAlerts
    {
        // ReSharper disable ConvertToConstant.Local
    	private readonly SpeechSynthesizer _synthesizer;
        private readonly int _delayBetweenAlertsInSeconds = 10;

        private bool _canSpeak = true;
        private bool _disposed;

        private DateTime _timeOfNextAlert = DateTime.Now;

        private readonly IConfiguration _configuration;
        private readonly IMeCache _meCache;
        private readonly ILogging _logging;

        public Alerts(IConfiguration configuration, IMeCache meCache, ILogging logging)
        {
            _logging = logging;

			try
			{
				_synthesizer = new SpeechSynthesizer();
                _synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
			}
			catch (Exception)
			{
				_logging.LogMessage("Alerts", "Initialize", LogSeverityTypes.Standard,
				                              "Unable to initialize SpeechSynthesizer. Text-to-Speech alerts will not be available.");
				_canSpeak = false;
			    _disposed = true;
			    return;
			}

            _configuration = configuration;
            _meCache = meCache;

			//Attach an eventhandler for speakCompleted and set the volume of the synth to max
			_synthesizer.SpeakCompleted += synthesizer_SpeakCompleted;
			_synthesizer.Volume = 100;

            //Make sure we get disposed
            ModuleManager.ModulesToDispose.Add(this);
        }

        ~Alerts()
        {
            Dispose(false);
        }

        #region IDisposable Implementors
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            _disposed = true;

            if (disposing)
            {
                //Suppress finalization
                GC.SuppressFinalize(this);
            }

            if (!_canSpeak)
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
            _synthesizer.Dispose();
        }
        #endregion

        private void synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            lock (this)
            {
                _timeOfNextAlert = Metatron.TimeOfPulse.AddSeconds(_delayBetweenAlertsInSeconds);
                _canSpeak = true;
            }
        }

        public void LocalUnsafe(string playerName, string corporationName, string allianceName)
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnLocalUnsafe &&
                _canSpeak &&
                Metatron.TimeOfPulse.CompareTo(_timeOfNextAlert) >= 0)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} local unsafe. Player {1}, corporation {2}, alliance {3}.",
                    _meCache.Name, playerName, corporationName, allianceName));
            }
        }

        public void LocalChat(string speakerName, string message)
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnLocalChat &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} local chat from {1}, message reads: {2}.",
                    _meCache.Name, speakerName, message));
            }
        }

        public void FactionSpawn(string spawnName)
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnFactionSpawn &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} faction spawn of {1}.",
                    _meCache.Name, spawnName));
            }
        }

        public void LowOnCharges()
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnLowAmmo &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} is low on charges.", _meCache.Name));
            }
        }

        public void NothingForFreighterToPickup()
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnFreighterNoPickup &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} has nothing to pick up.", _meCache.Name));
            }
        }

        public void LongRandomWait(int secondsToWait)
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnLongRandomWait &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} is waiting for {1} seconds.", _meCache.Name, secondsToWait));
            }
        }

        public void Fleeing(string fleeReason)
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnFlee &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} is fleeing because {1}.", _meCache.Name, fleeReason));
            }
        }

        public void PlayerNear()
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnPlayerNear &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} changing belts because player nearby.", _meCache.Name));
            }
        }

        public void TargetJammed()
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnTargetJammed &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} is target jammed.", _meCache.Name));
            }
        }

        public void WarpDisrupted()
        {
            if (_configuration.AlertConfig.UseAlerts &&
                _configuration.AlertConfig.AlertOnWarpJammed &&
                _canSpeak)
            {
                _canSpeak = false;
                _synthesizer.SpeakAsync(
                    String.Format("{0} is warp disrupted.", _meCache.Name));
            }
        }
        // ReSharper restore ConvertToConstant.Local
    }
}
