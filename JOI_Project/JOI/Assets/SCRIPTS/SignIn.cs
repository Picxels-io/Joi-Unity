using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SignIn : MonoBehaviour
{
    // Referencia al UIDocument desde el Inspector
    public UIDocument uiDocument;
    public ChangeScene changeSceneScript;
    private void Start()
    {
        // UiDocument existe?
        if (uiDocument != null)
        {
            // Obtener el elemento raíz del UIDocument
            VisualElement rootElement = uiDocument.rootVisualElement;

            // Obtener referencias a los botones usando sus identificadores únicos
            Button google = rootElement.Q<Button>("google");
            Button apple = rootElement.Q<Button>("apple");

            // Asignar comportamientos de clic a los botones
            google.clickable.clicked += OnGoogleClick;
            apple.clickable.clicked += OnAppleClick;
            Debug.Log("Asignados");
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
