using System;
using System.Collections.Generic;
using System.Linq;
using EVE.ISXEVE;
using System.IO;
using LavishScriptAPI;
using Metatron.Core.Interfaces;
using System.Globalization;
using System.Reflection;
using InnerSpaceAPI;

namespace Metatron.Core
{
    public sealed class AgentCache : ModuleBase, IAgentCache
    {
        // ReSharper disable ConvertToConstant.Local
        // ReSharper disable InconsistentNaming
    	private readonly string WindowcaptionAgentConversation = "Agent Conversation - ";
        private readonly string AgentCacheFileSuffix = " Agents.bin";

        private readonly string _oldDbFileName = "EVEDB_Agents.bin";
        private string _oldDbFilePath = String.Empty;

        List<CachedAgent> _cachedAgents = new List<CachedAgent>();

        //Callbacks for when the read / writes finish
    	private readonly FileReadCallback<CachedAgent> _readCallback;
    	private readonly FileWriteCallback _writeCallback;

        private readonly IFileManager _fileManager;
        private readonly IMeCache _meCache;
        private readonly IConfiguration _configuration;
        private readonly IEveWindowProvider _eveWindowProvider;
        private readonly IIsxeveProvider _isxeveProvider;

        internal AgentCache(IFileManager fileManager, IMeCache meCache, IConfiguration configuration, IEveWindowProvider eveWindowProvider, IIsxeveProvider isxeveProvider)
        {
            _fileManager = fileManager;
            _meCache = meCache;
            _configuration = configuration;
            _eveWindowProvider = eveWindowProvider;
            _isxeveProvider = isxeveProvider;

            ModuleName = "AgentCache";

            _readCallback = FileReadCallback;
            _writeCallback = FileWriteCallback;

            ModuleManager.ModulesToPulse.Add(this);
        }

        public override bool Initialize()
        {
            var methodName = "Initialize";
        	LogTrace(methodName);

			IsCleanedUpOutOfFrame = false;
            if (!IsInitialized)
            {
                //If we're not already initializing...
                if (!_isInitializing)
                {
                    //State that we are and start the agent cache load
                    _isInitializing = true;
                    LoadAgentCache();
                }
            }

            //Return whether or not we're initialized
            return IsInitialized;
        }

        public override bool OutOfFrameCleanup()
        {
            var methodName = "OutOfFrameCleanup";
			LogTrace(methodName);

            //If not already cleaned up...
            if (!IsCleanedUpOutOfFrame)
            {
                //If not cleaning up...
                if (!_isCleaningUp)
                {
                    //Only save if we've got agents to save
                    if (_cachedAgents.Count > 0)
                    {
                        _isCleaningUp = true;
                        SaveAgentCache(true);
                    }
                    else
                    {
                        IsCleanedUpOutOfFrame = true;
                    }
                }
            }

            return IsCleanedUpOutOfFrame;
        }

        #region Load/Save
        private void LoadAgentCache()
        {
            var methodName = "LoadAgentCache";
        	LogTrace(methodName);

            _oldDbFilePath = String.Format("{0}\\{1}", Metatron.ConfigDirectory, _oldDbFileName);

            var agentCacheFilePath = GetAgentCacheFilePath();

            if (File.Exists(_oldDbFilePath) && !File.Exists(agentCacheFilePath))
            {
				LogMessage(methodName, LogSeverityTypes.Standard, "Moving old Agent DB file to new per-user Agent DB file.");
                File.Move(_oldDbFilePath, agentCacheFilePath);
            }

            _fileManager.QueueDeserialize(agentCacheFilePath, _readCallback);
        }

        private void FileReadCallback(List<CachedAgent> result)
        {
            lock (_cachedAgents)
            {
                _cachedAgents = result;
            }

            //Set the initialized variable
            IsInitialized = true;
        }

		public void SaveAgentCache()
		{
			SaveAgentCache(false);
		}

        /// <summary>
        /// Save the list of CachedAgents to file.
        /// </summary>
        private void SaveAgentCache(bool useCallback)
        {
            var methodName = "SaveAgentCache";
        	LogTrace(methodName);

            var agentCacheFilePath = GetAgentCacheFilePath();

            _fileManager.QueueOverwriteSerialize(
                agentCacheFilePath, new List<CachedAgent>(_cachedAgents), useCallback ? _writeCallback : null);
        }

        private string GetAgentCacheFilePath()
        {
            var agentCacheFileName = String.Format("{0}{1}", _meCache.Name, AgentCacheFileSuffix);
            var agentCacheFilePath = String.Format("{0}\\{1}", Metatron.ConfigDirectory, agentCacheFileName);
            return agentCacheFilePath;
        }

        void FileWriteCallback()
        {
            IsCleanedUpOutOfFrame = true;
        }
        #endregion

        public override void Pulse()
        {
            var methodName = "Pulse";
            LogTrace(methodName);

            StartPulseProfiling();

            lock (_cachedAgents)
            {
                for (var index = 0; index < _cachedAgents.Count; index++)
                {
                    var cachedAgent = _cachedAgents[index];
                    if (string.IsNullOrEmpty(cachedAgent.Name))
                    {
                        _cachedAgents.RemoveAt(index);
                        index--;

                        LogMessage(methodName, LogSeverityTypes.Debug, "CachedAgent with ID {0} was invalid. Pruning it.", cachedAgent.Id);
                    }
                }
            }

            //Iterate all listed agents and research agents and prune any nulls
            for (var index = 0; index < _configuration.MissionConfig.Agents.Count; index++)
            {
                var agentName = _configuration.MissionConfig.Agents[index];
                if (GetCachedAgent(agentName) != null)
                    continue;

                LogMessage(methodName, LogSeverityTypes.Critical, "Agent \"{0}\" was invalid - we were unable to fetch an agent for this entry and are removing it.",
                    agentName);

                _configuration.MissionConfig.Agents.RemoveAt(index);
                index--;
            }

            for (var index = 0; index < _configuration.MissionConfig.ResearchAgents.Count; index++)
            {
                var researchAgentName = _configuration.MissionConfig.ResearchAgents[index];
                if (GetCachedAgent(researchAgentName) != null)
                    continue;

                LogMessage(methodName, LogSeverityTypes.Critical, "Research Agent \"{0}\" was invalid - we were unable to fetch an agent for this entry and are removing it.",
                    researchAgentName);

                _configuration.MissionConfig.ResearchAgents.RemoveAt(index);
                index--;
            }

            EndPulseProfiling();
        }

        public CachedAgent GetCachedAgent(string agentName)
        {
            var methodName = "GetCachedAgent";
			LogTrace(methodName, "AgentName: {0}", agentName);

            if (string.IsNullOrEmpty(agentName))
                throw new ArgumentException("AgentName cannot be null or empty.", "agentName");

            //Iterate and look for a match.
            var cachedAgent = _cachedAgents.FirstOrDefault(ca => ca.Name.Equals(agentName, StringComparison.InvariantCultureIgnoreCase));
            if (cachedAgent != null) return cachedAgent;

            //No match? Add a cachedagent.
            LogMessage(methodName, LogSeverityTypes.Debug, "Caching agent with name \"{0}\".", agentName);
            var newCachedAgent = CreateCachedAgent(agentName);
            if (newCachedAgent == null)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not fetch agent for agent name \"{0}\".", agentName);
                return null;
            }

            _cachedAgents.Add(newCachedAgent);
            return newCachedAgent;
        }

        public CachedAgent GetCachedAgent(int agentId)
        {
            var methodName = "GetCachedAgent";
			LogTrace(methodName, "AgentId: {0}", agentId);

            if (agentId <= 0)
                throw new ArgumentException("AgentId must be greater than 0.", "agentId");

            //Iterate and look for a match.
            var cachedAgent = _cachedAgents.FirstOrDefault(ca => ca.Id == agentId);
            if (cachedAgent != null) return cachedAgent;

            //no match? Add a cachedagent.
            LogMessage(methodName, LogSeverityTypes.Debug, "Caching agent with ID {0}.", agentId);
            var newCachedAgent = CreateCachedAgent(agentId);
            if (newCachedAgent == null)
            {
                LogMessage(methodName, LogSeverityTypes.Standard, "Error: Could not fetch agent for agent ID {0}.", agentId);
                return null;
            }

            _cachedAgents.Add(newCachedAgent);
            return newCachedAgent;
        }

        public EVEWindow GetAgentConversationWindow(int agentId)
        {
            var methodName = "GetAgentConversationWindow";
            LogTrace(methodName, "AgentId: {0}", agentId);

            if (agentId <= 0)
                throw new ArgumentException("AgentId must be greater than 0.", "agentId");

            //Get a cached agent for the given ID if possible.
            var cachedAgent = GetCachedAgent(agentId);

            var missionWindowCaptionString = string.Format("Agent Conversation - {0}", cachedAgent.Name);

            var agentMissionWindow = _eveWindowProvider.GetWindowByCaption(missionWindowCaptionString);
            return agentMissionWindow;
        }

        /// <summary>
        /// Creates a CachedAgent object using a given agent name.
        /// </summary>
        /// <param name="agentName">Name of the agent to get.</param>
        /// <returns>CachedAgent object representing said agent.</returns>
        public CachedAgent CreateCachedAgent(string agentName)
        {
            var methodName = "CreateCachedAgent";
            LogTrace(methodName, "AgentName: {0}", agentName);

            //Get the ISXEVE agent using the passed name. This is a server call
            //and will take ages.
            //var agent = Agent.ByName(agentName);
            var agent = new Agent(_isxeveProvider.Eve.GetMember("Agent", agentName));
            //InnerSpace.Echo(agent.Index.ToString());

            //Get its cached representation.
            if (LavishScriptObject.IsNullOrInvalid(agent)) return null;

            var cachedAgent = new CachedAgent(agent);
            return cachedAgent;
        }

        /// <summary>
        /// Creates a CachedAgent object using a given agent ID.
        /// </summary>
        /// <param name="agentId">ID of the agent to get.</param>
        /// <returns>CachedAgent object representing said agent.</returns>
        public CachedAgent CreateCachedAgent(int agentId)
        {
            var methodName = "CreateCachedAgent";
            LogTrace(methodName, "AgentId: {0}", agentId);

            //Get the ISXEVE agent using the passed ID. This is also a long-ass server call.
            //No longer! Not going to be using Index any more.
            var agent = new Agent(_isxeveProvider.Eve.GetMember("Agent", "id", agentId.ToString(CultureInfo.InvariantCulture)));
            //var agent = new Agent(LavishScript.Objects.GetObject("EVE", "Agent", "id", agentId.ToString(CultureInfo.CurrentCulture)));
            //InnerSpace.Echo(agent.Index.ToString());


            if (LavishScriptObject.IsNullOrInvalid(agent)) return null;

            //Get its cached representation.
            var cachedAgent = new CachedAgent(agent);
            return cachedAgent;
        }

        /// <summary>
        /// Get the Agent object this CachedAgent represents.
        /// </summary>
        public Agent GetAgent(CachedAgent cachedAgent)
        {
            var agent = new Agent(_isxeveProvider.Eve.GetMember("Agent", "id", cachedAgent.Id.ToString(CultureInfo.InvariantCulture)));
            ModuleManager.HomelessLsosToDispose.Add(agent);
            return agent;
        }
    }
    // ReSharper restore InconsistentNaming
    // ReSharper restore ConvertToConstant.Local
}
