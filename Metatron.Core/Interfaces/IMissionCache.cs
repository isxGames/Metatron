using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using EVE.ISXEVE;

namespace Metatron.Core.Interfaces
{
    public interface IMissionCache
    {
        bool UpdateCache();
        bool UpdateCache(bool fastUpdate);
        bool UpdateCache(int agentId);
        bool UpdateCache(int agentId, bool fastUpdate);
        ConversationStateResults GetMissionFromAgent(int agentId);
        bool TurnInMission(CachedAgent cachedAgent);
        CachedMission GetCachedMissionForAgentId(int agentId);
        AgentMission GetAgentMissionFromAgent(int agentId);
        void ClearMissionFromAgent(int agentId);
        AgentMission GetAgentMission(CachedMission cachedMission);
        bool IsAtMissionStartBookmark(CachedMission cachedMission);
        bool IsAtMissionEndBookmark(CachedMission cachedMission);
        List<BookMark> GetMissionBookmarks(CachedMission cachedMission);
        BookMark GetMissionStartBookmark(CachedMission cachedMission);
        BookMark GetMissionEndBookmark(CachedMission cachedMission);
        BookMark GetBookmarkMatchingTag(CachedMission cachedMission, string tagString);
        bool IsMissionAcceptible(CachedMission cachedMission);
        bool IsStorylineMission(string type);
        ReadOnlyCollection<CachedMission> CachedMissions { get; }
    }

    public enum UpdateCacheStates
    {
        Idle,
        OpenJournal,
        CheckMissions,
        Cleanup
    }

    public enum CheckMissionStates
    {
        Idle,
        OpenAgentConversation,
        OpenMission,
        ParseMission,
        Cleanup
    }

    public enum GetMissionStates
    {
        Idle,
        OpenJournal,
        OpenAgentConversation,
        UpdateCachedMission,
        AcceptMission,
        DeclineMission,
        VerifyMission,
        Cleanup
    }

    public enum ConversationStateResults
    {
        Incomplete,
        MissionAccepted,
        MissionNotAccepted,
        DeclineFailed
    }
}
