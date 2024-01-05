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
        authenticationManagerScript.StartAuthentication();
        //changeSceneScript.ChangeToScene("2");
        // Espera un breve momento (por ejemplo, 1 segundo) y luego verifica el estado de autenticación
        //StartCoroutine(CheckAuthenticationStatus());
    }

    private IEnumerator CheckAuthenticationStatus()
    {
        yield return new WaitForSeconds(6); // Espera 1 segundo para asegurarse de que la corutina haya terminado

        if (authenticationManagerScript.IsAuthenticationSuccessful())
        {
            // Si la autenticación fue exitosa, procede al siguiente paso
            authenticationManagerScript.ResetAuthenticationStatus(); // Restablecer el estado de autenticación para futuras solicitudes
            Debug.Log("Autenticación exitosa, procediendo al siguiente paso.");
            // Aquí puedes llamar a cualquier función o acción que desees después de una autenticación exitosa
        }
        else
        {
            // Si la autenticación no fue exitosa, manejar el error o tomar medidas adecuadas
            Debug.LogError("Autenticación no exitosa.");
        }
    }

    private void OnAppleClick()
    {
        Debug.Log("Se hizo clic en Apple.");
        changeSceneScript.ChangeToScene("2");
    }
}
