using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetOnStart : MonoBehaviour
{
    public static ResetOnStart Instance;
    public int sceneId;

    void Awake()
    {
        sceneId = SceneManager.GetActiveScene().buildIndex;

        if (Instance != null && Instance.sceneId == sceneId) 
        { 
            Destroy(gameObject); 
            return; 
        }
        else if(Instance != null && Instance.sceneId != sceneId)
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;

        GameObject music = GameObject.FindGameObjectWithTag("Music");
        if (music != null) 
        {
            Destroy(music);
        }

        GameObject ambience = GameObject.FindGameObjectWithTag("Ambience");
        if (ambience != null)
        {
            Destroy(ambience);
        }

        PlayerPrefs.SetString("coins", "");
        PlayerPrefs.SetString("equippedpowerups", "");
        PlayerPrefs.SetInt("wallet", 0);
    }
}
