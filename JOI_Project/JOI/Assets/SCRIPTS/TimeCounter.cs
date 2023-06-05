using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeCounter : MonoBehaviour
{
    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update()
    {
        TimeSpan time = TimeSpan.FromSeconds(Time.timeSinceLevelLoad);
        _text.text = time.ToString(@"mm\:ss");
    }
}