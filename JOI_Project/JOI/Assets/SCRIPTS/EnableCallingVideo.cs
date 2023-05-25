using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnableCallingVideo : MonoBehaviour
{
    public GameObject panel;
    private IEnumerator DisablePanelAsync()
    {
        yield return new WaitForSeconds(1.5f);
        panel.SetActive(false);
    }
    private void Awake()
    {
        StartCoroutine(DisablePanelAsync());
    }
}
