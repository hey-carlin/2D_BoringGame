using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonKIT
{
    /// <summary>
    /// Wrapper so Unity can serialize a string[] for each dialog stage in Guide UI mode.
    /// </summary>
    [System.Serializable]
    public class GuideDialogStage
    {
        [Multiline]
        public string[] dialogs;
    }

    public class NPC : MonoBehaviour
    {
        [Header("NPC Identity")]
        [Tooltip("Unique ID shared by all instances of this character across scenes.")]
        public string npcId;

        [Header("Staged Dialog (multi-encounter)")]
        [Tooltip("Enable to use dialogStages / guideDialogStages based on game progress.")]
        public bool useStages;
        [Tooltip("One config per encounter (DialogConfig mode). Index = encounter number.")]
        public DialogConfig[] dialogStages;
        [Tooltip("One entry per encounter (Guide UI mode). Index = encounter number.")]
        public GuideDialogStage[] guideDialogStages;
        [Tooltip("Automatically advance to next stage after this conversation.")]
        public bool autoAdvanceStage = true;

        [Header("Single Dialog (used when useStages = false)")]
        public DialogConfig dialogConfig; //config containing the text of the dialogue
        public bool useGuideUI; //Use the guide dialog UI panel instead of default
        public string[] guideDialogs; //Dialog texts edited directly here, no ScriptableObject needed

        InteractionTrigger interactionTrigger; // interaction trigger

        private void Start()
        {
            interactionTrigger = GetComponent<InteractionTrigger>();
        }

        private void Update()
        {
            // Don't trigger interaction if dialog/shop UI is already open
            if (UIManager.Instance.isPause) return;

            if (interactionTrigger.inTrigger)//if player in trigger
            {
                if (InputManager.Interaction) // if player press Interaction button
                {
                    InputManager.Interaction = false;
                    Interaction(); //Interaction
                }
            }
        }

        //Interaction method
        void Interaction()
        {
            if (useStages && !string.IsNullOrEmpty(npcId))
            {
                int stage = GameProgressManager.Instance != null
                    ? GameProgressManager.Instance.GetStage(npcId)
                    : 0;

                // Try DialogConfig stages first
                if (dialogStages != null && stage < dialogStages.Length && dialogStages[stage] != null)
                {
                    UIManager.Instance.ShowDialogMenu(dialogStages[stage]);
                }
                // Then try Guide UI stages
                else if (guideDialogStages != null && stage < guideDialogStages.Length
                    && guideDialogStages[stage] != null && guideDialogStages[stage].dialogs != null
                    && guideDialogStages[stage].dialogs.Length > 0)
                {
                    UIManager.Instance.ShowGuideDialog(guideDialogStages[stage].dialogs);
                }
                else
                {
                    // Out of staged dialogs — fall back to original single config
                    ShowOriginalDialog();
                    return;
                }

                // Advance to next stage for the next encounter
                if (autoAdvanceStage && GameProgressManager.Instance != null)
                    GameProgressManager.Instance.AdvanceStage(npcId);
            }
            else
            {
                ShowOriginalDialog();
            }
        }

        void ShowOriginalDialog()
        {
            if (useGuideUI)
                UIManager.Instance.ShowGuideDialog(guideDialogs);
            else
                UIManager.Instance.ShowDialogMenu(dialogConfig);
        }
    }
}
