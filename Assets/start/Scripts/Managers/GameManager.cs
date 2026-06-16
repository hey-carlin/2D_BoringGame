using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonKIT
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance; //Singleton

        NextLevelDoor nextLevelDoor; //Next level door object

        [Header("Settings")]
        public bool isGame = true;
        public bool levelComplete;

        private void Awake()
        {
            //Set sigleton
            Instance = this;
        }

        private void Start()
        {
            //Find next level door
            GameObject doorObj = GameObject.Find("NextLevelDoor");
            if (doorObj != null)
            {
                nextLevelDoor = doorObj.GetComponent<NextLevelDoor>();
            }
        }

        //GameOver method
        public void GameOver()
        {
            isGame = false; //Game status is false, and all actions on scene is stop
            UIManager.Instance.GameOver(); //Show GameOver screen

            // 播放失败音乐
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
                AudioManager.Instance.PlaySFX(AudioManager.Instance.defeatMusic);
            }
        }

        //Complete level method
        public void LevelComplete()
        {
            levelComplete = true; //Set bool for Check door status
            if (nextLevelDoor != null)
            {
                nextLevelDoor.lockedDoor = false; //Unlock door
                nextLevelDoor.CheckLockStatus(); //Check door status
            }

            // 播放胜利音乐
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
                AudioManager.Instance.PlaySFX(AudioManager.Instance.victoryMusic);
            }
        }
    }
}