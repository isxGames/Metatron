using Metatron.Core.CustomEventArgs;
using Metatron.Core;
using Metatron.Core.Interfaces;
using LavishScriptAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Metatron
{
    public partial class MiniStatusForm : Form
    {
        public MiniStatusForm()
        {
            InitializeComponent();
            AttachEventHandlers();
        }
        private EventHandler<LogEventArgs> _logMessageEventHandler;
        private EventHandler _configLoadedEventHandler;
        private EventHandler<LSEventArgs> _onPulse;
        private EventHandler<PairEventArgs<string, int>> _statisticsOnAddIceOreMinedEventHandler;
        private EventHandler<PairEventArgs<string, int>> _statisticsOnCrystalsUsedEventHandler;
        private EventHandler<TimeSpanEventArgs> _statisticsOnMiningCargoFullEventHandler;
        private EventHandler<ManuallyAddPilotEventArgs> _manuallyAddEntryFormManuallyAddEntry;
        private EventHandler<__err_retn> _authenticationCompleted;
        private EventHandler<WalletStatisticsUpdatedEventArgs> _walletStatisticsUpdated;
        private EventHandler _exitDelegate;

        private void pauseButton_Click(object sender, EventArgs e)
        {
            if (Core.Metatron.Instance.IsEnabled)
            {
                pauseButton.BackColor = Color.Orange;
                Core.Metatron.Instance.IsEnabled = false;
                Core.Metatron.Logging.LogMessage("MiniStatusForm", "Pause", LogSeverityTypes.Standard,
                    "Pausing");
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            Core.Metatron.Instance.IsEnabled = true;
            pauseButton.BackColor = Color.Green;
            if (startButton.Text == "Start")
            {
                startButton.Text = "Resume";
            }
            Core.Metatron.JustLoadConfig = false;
            Core.Metatron.Logging.LogMessage("MetatronForm", "Start", LogSeverityTypes.Standard,
                "Enabling full pulse");
        }
        private void AttachEventHandlers()
        {
            _logMessageEventHandler = Logging_LogMessage;
            Core.Metatron.Logging.MessageLogged += _logMessageEventHandler;

            //_configLoadedEventHandler = Configuration_ConfigLoaded;
            //Core.Metatron.ConfigurationManager.ConfigLoaded += _configLoadedEventHandler;

            _onPulse = Pulse;
            Core.Metatron.OnPulse += _onPulse;

            //_walletStatisticsUpdated = WalletStatisticsUpdated;
            //Core.Metatron.Statistics.WalletStatisticsUpdated += _walletStatisticsUpdated;

            //_statisticsOnAddIceOreMinedEventHandler = Statistics_OnAddIceOreMined;
            //Core.Metatron.Statistics.OnAddIceOreMined += _statisticsOnAddIceOreMinedEventHandler;

            //_statisticsOnCrystalsUsedEventHandler = Statistics_OnCrystalsUsed;
            //Core.Metatron.Statistics.OnCrystalsUsed += _statisticsOnCrystalsUsedEventHandler;

            //_statisticsOnMiningCargoFullEventHandler = Statistics_OnMiningCargoFull;
            //Core.Metatron.Statistics.OnDropoff += _statisticsOnMiningCargoFullEventHandler;

            Core.Metatron.SaveAndExit += Metatron_SaveAndExit;

            _exitDelegate = Metatron_SaveAndExit;
        }

        private void Pulse(object sender, LSEventArgs e)
        {
            {
                shipNameLabel.Text = $"Ship: {Core.Metatron.MeCache.Ship.Name}";
            }
            {
                string shieldCalc = $"{(int)Core.Metatron.MeCache.Ship.Shield}/{(int)Core.Metatron.MeCache.Ship.MaxShield} ({(int)Core.Metatron.MeCache.Ship.ShieldPct}%)";
                shieldLabel.Text = $"Shields: {shieldCalc}";
            }
            {
                string armorCalc = $"{(int)Core.Metatron.MeCache.Ship.Armor}/{(int)Core.Metatron.MeCache.Ship.MaxArmor} ({(int)Core.Metatron.MeCache.Ship.ArmorPct}%)";
                armorLabel.Text = $"Armor: {armorCalc}";
            }
            {
                string hullCalc = $"{(int)Core.Metatron.MeCache.Ship.Ship.Structure}/{(int)Core.Metatron.MeCache.Ship.Ship.MaxStructure} ({(int)Core.Metatron.MeCache.Ship.StructurePct}%)";
                structureLabel.Text = $"Hull: {hullCalc}";
            }
            {
                this.Text = $"Metatron | {Core.Metatron.MeCache.Name} - {Core.Metatron.ConfigurationManager.ActiveConfigName}";
            }
        }

        private void Metatron_SaveAndExit(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            DetachMetatronToInterfaceEvents();
            base.OnFormClosing(e);
        }

        private void DetachMetatronToInterfaceEvents()
        {
            Core.Metatron.Logging.MessageLogged -= _logMessageEventHandler;
            //Core.Metatron.ConfigurationManager.ConfigLoaded -= _configLoadedEventHandler;
            Core.Metatron.OnPulse -= _onPulse;
            //_auth.AuthenticationComplete -= _authenticationCompleted;
            //Core.Metatron.Statistics.WalletStatisticsUpdated -= _walletStatisticsUpdated;
            //Core.Metatron.Statistics.OnAddIceOreMined -= _statisticsOnAddIceOreMinedEventHandler;
            //Core.Metatron.Statistics.OnCrystalsUsed -= _statisticsOnCrystalsUsedEventHandler;
            //Core.Metatron.Statistics.OnDropoff -= _statisticsOnMiningCargoFullEventHandler;
            Core.Metatron.SaveAndExit -= Metatron_SaveAndExit;
        }


        private void Logging_LogMessage(object sender, LogEventArgs e)
        {
            if (Disposing || IsDisposed)
                return;

            if (e.Severity == LogSeverityTypes.Debug ||
                (!Core.Metatron.IsDebug && e.Severity == LogSeverityTypes.Debug) ||
                e.Severity == LogSeverityTypes.Trace ||
                e.Severity == LogSeverityTypes.Profiling)
                return;

            if (InvokeRequired)
            {
                Invoke(_logMessageEventHandler, this, e);
                return;
            }

            listBoxLogMessages.Items.Add(e.FormattedMessage);
            while (listBoxLogMessages.Items.Count > 100)
            {
                listBoxLogMessages.Items.RemoveAt(0);
            }
            Invalidate();
            listBoxLogMessages.SelectedIndex = listBoxLogMessages.Items.Count - 1;
        }

    }
}
