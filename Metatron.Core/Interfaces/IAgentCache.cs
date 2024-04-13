using EVE.ISXEVE;

namespace Metatron.Core.Interfaces
{
    public interface IAgentCache
    {
        void SaveAgentCache();
        CachedAgent GetCachedAgent(string agentName);
        CachedAgent GetCachedAgent(int agentId);
        EVEWindow GetAgentConversationWindow(int agentId);

        /// <summary>
        /// Creates a CachedAgent object using a given agent name.
        /// </summary>
        /// <param name="agentName">Name of the agent to get.</param>
        /// <returns>CachedAgent object representing said agent.</returns>
        CachedAgent CreateCachedAgent(string agentName);

        /// <summary>
        /// Creates a CachedAgent object using a given agent ID.
        /// </summary>
        /// <param name="agentId">ID of the agent to get.</param>
        /// <returns>CachedAgent object representing said agent.</returns>
        CachedAgent CreateCachedAgent(int agentId);

        /// <summary>
        /// Get the Agent object this CachedAgent represents.
        /// </summary>
        Agent GetAgent(CachedAgent cachedAgent);
    }
}
