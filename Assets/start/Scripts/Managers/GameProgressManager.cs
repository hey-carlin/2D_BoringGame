using System.Collections.Generic;
using UnityEngine;

namespace DungeonKIT
{
    /// <summary>
    /// Global singleton that tracks game progress across scenes.
    /// Tracks how many times each NPC has been spoken to, so the same character
    /// can appear in different scenes and show the correct dialog for that encounter.
    /// </summary>
    public class GameProgressManager : MonoBehaviour
    {
        public static GameProgressManager Instance;

        // Maps npcId → current dialog stage (how many times encountered)
        [SerializeField]
        private List<string> npcIdKeys = new List<string>();
        [SerializeField]
        private List<int> npcStageValues = new List<int>();

        // Runtime dictionary (rebuilt from serialized lists after scene load)
        private Dictionary<string, int> npcStages = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Rebuild dictionary from serialized lists
            RebuildDictionary();
        }

        private void RebuildDictionary()
        {
            npcStages.Clear();
            for (int i = 0; i < npcIdKeys.Count && i < npcStageValues.Count; i++)
            {
                npcStages[npcIdKeys[i]] = npcStageValues[i];
            }
        }

        private void SyncToSerialized()
        {
            npcIdKeys.Clear();
            npcStageValues.Clear();
            foreach (var kvp in npcStages)
            {
                npcIdKeys.Add(kvp.Key);
                npcStageValues.Add(kvp.Value);
            }
        }

        /// <summary>
        /// Get the current dialog stage for an NPC (0 = first encounter, 1 = second, etc.)
        /// </summary>
        public int GetStage(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return 0;
            if (npcStages.TryGetValue(npcId, out int stage))
                return stage;
            return 0;
        }

        /// <summary>
        /// Set a specific stage for an NPC (for quests, events, etc.)
        /// </summary>
        public void SetStage(string npcId, int stage)
        {
            if (string.IsNullOrEmpty(npcId)) return;
            npcStages[npcId] = stage;
            SyncToSerialized();
        }

        /// <summary>
        /// Advance to the next dialog stage (after a conversation ends)
        /// </summary>
        public void AdvanceStage(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return;
            if (npcStages.ContainsKey(npcId))
                npcStages[npcId]++;
            else
                npcStages[npcId] = 1;
            SyncToSerialized();
        }

        /// <summary>
        /// Reset all NPC progress (for New Game)
        /// </summary>
        public void ResetAll()
        {
            npcStages.Clear();
            npcIdKeys.Clear();
            npcStageValues.Clear();
        }
    }
}
