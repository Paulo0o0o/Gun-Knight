using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    void Start()
    {
        bool a = PlayerPrefs.GetString("coins").Contains(gameObject.name);
        bool b = PlayerPrefs.GetString("equippedpowerups").Contains(gameObject.name);
        if (a)
        {
            gameObject.SetActive(false);
        }
        else if (b)
        {
            GameObject player = GameObject.Find("Player");
            Tooltip childPUPScript = GetComponentInChildren<Tooltip>();
            TooltipManager._instance.powerupEffect = childPUPScript.powerupEffect;
            TooltipManager._instance.player = player;
            TooltipManager._instance.ApplyEffect();
            TooltipManager._instance.powerupEffect = null;
            TooltipManager._instance.player = null;
            gameObject.SetActive(false);
        }
    }
}
