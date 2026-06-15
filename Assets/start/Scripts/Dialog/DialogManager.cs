using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonKIT
{
    public class DialogManager : MonoBehaviour
    {
        [Header("Default Dialog UI (uses DialogConfig .asset)")]
        public Text dialogNameText, dialogText;

        [Header("Guide Dialog UI (NPC1, text edited directly on NPC)")]
        public Text dialogText_Guide;

        //Current dialog state
        DialogConfig currentConfig;
        string[] currentGuideDialogs;
        int currentIndex;
        bool isGuideUI;

        /// <summary>
        /// Set up dialog with default UI panel (uses DialogConfig ScriptableObject)
        /// </summary>
        public void SetDialogConfig(DialogConfig dialogConfig)
        {
            isGuideUI = false;
            currentConfig = dialogConfig;
            currentIndex = 0;
            dialogNameText.text = dialogConfig.name;
            dialogText.text = dialogConfig.dialogs[0].dialogText;
        }

        /// <summary>
        /// Set up guide dialog directly from string array (no .asset needed)
        /// </summary>
        public void SetGuideDialog(string[] dialogs)
        {
            isGuideUI = true;
            currentGuideDialogs = dialogs;
            currentIndex = 0;
            if (dialogText_Guide != null && dialogs != null && dialogs.Length > 0)
                dialogText_Guide.text = dialogs[0];
        }

        /// <summary>
        /// Advance to next dialog. Returns true if there is a next dialog, false if reached the end.
        /// </summary>
        public bool NextDialog()
        {
            currentIndex++;
            if (isGuideUI)
            {
                if (currentGuideDialogs != null && currentIndex < currentGuideDialogs.Length)
                {
                    if (dialogText_Guide != null)
                        dialogText_Guide.text = currentGuideDialogs[currentIndex];
                    return true;
                }
            }
            else
            {
                if (currentConfig != null && currentIndex < currentConfig.dialogs.Length)
                {
                    dialogText.text = currentConfig.dialogs[currentIndex].dialogText;
                    return true;
                }
            }
            return false;
        }


    }
}
