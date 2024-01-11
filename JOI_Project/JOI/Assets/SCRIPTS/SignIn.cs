using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SignIn : MonoBehaviour
{
    public UIDocument uiDocument;
    public ChangeScene changeSceneScript;
    public AuthenticationManager authenticationManagerScript;

    private void Start()
    {
        if (uiDocument != null)
        {
            VisualElement rootElement = uiDocument.rootVisualElement;

            Button google = rootElement.Q<Button>("google");
            Button apple = rootElement.Q<Button>("apple");

            google.clickable.clicked += OnGoogleClick;
            apple.clickable.clicked += OnAppleClick;
        }
        else
        {
            Debug.LogError("UIDocument no está asignado en el Inspector de Unity.");
        }
    }

    private void OnGoogleClick()
    {
        Debug.Log("Se hizo clic en Google.");
        changeSceneScript.ChangeToScene("2");
    }

    private void OnAppleClick()
    {
        Debug.Log("Se hizo clic en Apple.");
        changeSceneScript.ChangeToScene("2");
    }
}
