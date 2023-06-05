using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeToVideoCall : MonoBehaviour
{
    public List<GameObject> toEnable;
    public List<GameObject> toDisable;

    public void EnableVideoCall()
    {
        foreach (GameObject obj in toEnable)
        {
            obj.SetActive(true);
        }

        foreach (GameObject obj in toDisable)
        {
            obj.SetActive(false);
        }
    }
}