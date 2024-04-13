using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using InnerSpaceAPI;
using Piwik.Tracker;

namespace Metatron.Core.Analytics
{
    public class AnalyticsProvider
    {
        private readonly PiwikTracker _tracker;
        private DateTime _lastPingTime = DateTime.MinValue;

        public AnalyticsProvider()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _tracker = new PiwikTracker(2, "https://nostrathomasindustries.matomo.cloud/matomo.php");
            _tracker.SetUserAgent($"Metatron ISX Application {GetAssemblyVersion()} {GetOSVersion()}");
            _tracker.SetCustomVariable(1, "Software Version", GetAssemblyVersion(), Scopes.Visit);
            _tracker.SetCustomVariable(2, "OS Version", GetOSVersion(), Scopes.Visit);
            _tracker.SetUserId(GetUserID());
            _tracker.EnableBulkTracking();
        }

        public void TrackEvent(Analytics.Events.BaseEvent eventObj)
        {
            if (eventObj == null) return;
            if (_tracker != null)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        Task.Run(() => _tracker.DoTrackEvent(eventObj.Category, eventObj.Action, eventObj.Label, eventObj.Value));
                    }
                    catch (Exception ex)
                    {
                        // Log the exception for debugging
                        InnerSpace.Echo("Error while tracking event: " + ex.ToString());
                    }
                });
            }
        }

        public void DoPing()
        {
            if (_tracker == null) return;
            if (_lastPingTime < DateTime.Now + TimeSpan.FromSeconds(60))
            {
                Task.Run(() => _tracker.DoBulkTrack());
                Task.Run(() => _tracker.DoPing());
                _lastPingTime = DateTime.Now;
            }
        }

        private string GetUserID()
        {
            // Get the username or some other non-identifiable information
            string hashable = Environment.UserName + Environment.MachineName + Environment.OSVersion.VersionString;

            // Hash the username to create an anonymous ID
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(hashable));
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private string GetOSVersion()
        {
            return Environment.OSVersion.VersionString;
        }

        private string GetAssemblyVersion()
        {
            // Get the current assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get the assembly name
            AssemblyName assemblyName = assembly.GetName();

            // Get the version
            Version version = assemblyName.Version;

            return version.ToString();
        }
    }
}
