using System;
using System.Text;
using LavishScriptAPI;
using Metatron.Core.Interfaces;

namespace Metatron.Core.EventCommunication
{
    public abstract class BaseEvent<T> : IDisposable where T : BaseEventArgs
    {
// ReSharper disable InconsistentNaming
        protected readonly string _lsEventName;
        protected readonly string _relayGroup;
        protected readonly bool _onlySendToOthers;
        private bool _isDisposed;

        private EventHandler<LSEventArgs> _internalEventRaised;
        public EventHandler<T> EventRaised;

        protected readonly ILogging _logging;
// ReSharper restore InconsistentNaming

        protected BaseEvent(ILogging logging, string relayGroup, string lsEventName, bool onlySendToOthers)
        {
            _relayGroup = relayGroup;
            _lsEventName = lsEventName;
            _onlySendToOthers = onlySendToOthers;
            _logging = logging;

            RegisterEvent();            
        }

        protected BaseEvent(ILogging logging, string relayGroup, string lsEventName) : this(logging, relayGroup, lsEventName, true) { }

        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;

            UnregisterEvent();

            //Ensure there are no listeners still attached to prevent memleaking
            EventRaised = null;
        }

        protected void RegisterEvent()
        {
            _internalEventRaised = RaiseEvent;
            LavishScript.Events.AttachEventTarget(_lsEventName, _internalEventRaised);
        }

        protected void UnregisterEvent()
        {
            LavishScript.Events.DetachEventTarget(_lsEventName, _internalEventRaised);
            _internalEventRaised = null;
        }

        protected void RaiseEvent(object sender, LSEventArgs e)
        {
            var eventArgs = GetEventArgs(e);

            _logging.LogTrace(this, _lsEventName, "Received event. Parameters: {0}", eventArgs.GetFieldCsv());

            if (EventRaised == null) return;

            EventRaised(sender, eventArgs);
        }

        protected abstract T GetEventArgs(LSEventArgs e);

        public void SendEvent(T e)
        {
            var fieldCsv = e.GetFieldCsv();

            SendEvent(fieldCsv);
        }

        public void SendEventFromArgs(params object[] args)
        {
            var stringBuilder = new StringBuilder();

            if (args != null)
            {
                foreach (var arg in args)
                {
                    if (stringBuilder.Length > 0)
                        stringBuilder.Append(',');

                    stringBuilder.Append(arg);
                }
            }

            var messageParameters = stringBuilder.ToString();
            SendEvent(messageParameters);
        }

        private void SendEvent(string eventData)
        {
            var eventString = String.Format("relay \"{0}{1}\" \"Event[{2}]:Execute[{3}]\"",
                _onlySendToOthers ? "other " : "", _relayGroup, _lsEventName, eventData);

            _logging.LogTrace(this, _lsEventName, "Sending event. Command: {0}", eventString);
            LavishScript.ExecuteCommand(eventString);
        }
    }

    public class BaseEventArgs : LSEventArgs
    {
        public Int64 SendingFleetMemberCharId { get; private set; }
        public int SolarSystemId { get; private set; }
        public string ObjectName { get; set; }

// ReSharper disable InconsistentNaming
        protected readonly ILogging _logging;
// ReSharper restore InconsistentNaming

        public BaseEventArgs(ILogging logging)
        {
            _logging = logging;
        }

        public BaseEventArgs(ILogging logging, long sendingFleetMemberId, int solarSystemId)
        {
            ObjectName = "BaseEventArgs";
            SendingFleetMemberCharId = sendingFleetMemberId;
            SolarSystemId = solarSystemId;

            _logging = logging;
        }

        public BaseEventArgs(ILogging logging, LSEventArgs copy)
        {
            _logging = logging;

            ObjectName = "BaseEventArgs";
            Initialize(copy);
        }

        protected virtual void Initialize(LSEventArgs copy)
        {
            var methodName = "Initialize";

            Int64 sendingFleetMemberId;
            int solarSystemId;

            if (!Int64.TryParse(copy.Args[0], out sendingFleetMemberId))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse sending fleet member ID {0}",
                    copy.Args[0]);
            }

            if (!int.TryParse(copy.Args[1], out solarSystemId))
            {
                _logging.LogMessage(ObjectName, methodName, LogSeverityTypes.Standard, "Unable to parse sending solar system ID {0}",
                    copy.Args[1]);
            }

            SendingFleetMemberCharId = sendingFleetMemberId;
            SolarSystemId = solarSystemId;
        }

        public virtual string GetFieldCsv()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(SendingFleetMemberCharId);
            stringBuilder.Append(',');
            stringBuilder.Append(SolarSystemId);

            return stringBuilder.ToString();
        }
    }
}
