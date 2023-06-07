using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeStage : MonoBehaviour
{
    public static ChangeStage Instance;

    [SerializeField] private List<GameObject> _stages;

    private int _currentActiveStage = 0;
    private float lastTapTime;
    private const float doubleTapTimeThreshold = 0.3f;

    public void ChangeScenario()
    {
        _currentActiveStage = (_currentActiveStage + 1) % _stages.Count;
        EnableStage(_currentActiveStage);
    }

    private void EnableStage(int stageIndex)
    {
        foreach (GameObject stage in _stages)
        {
            stage.SetActive(false);
        }

        _stages[stageIndex].SetActive(true);
    }

    private void Awake()
    {
        Instance = this;
    }
}