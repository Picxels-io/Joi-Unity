using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AuthenticationManager : MonoBehaviour
{
    private string loginUrl = "https://asktai.azurewebsites.net/.auth/login/google";
    private bool isAuthenticationSuccessful = false; // Variable para almacenar el estado de la autenticación

    // Método para iniciar el proceso de autenticación
    public void StartAuthentication()
    {
        StartCoroutine(AuthenticateWithGoogle());
    }

    private IEnumerator AuthenticateWithGoogle()
    {
        // 1. Abre la URL de autenticación en un navegador
        Application.OpenURL(loginUrl);

        // Espera un tiempo determinado (por ejemplo, 10 segundos) para que el usuario inicie sesión y proporcione la autenticación
        yield return new WaitForSeconds(10);

        // 2. Realiza la solicitud UnityWebRequest para verificar si la autenticación fue exitosa
        using (UnityWebRequest webRequest = new UnityWebRequest(loginUrl + "/callback", "POST"))
        {
            // Configura los encabezados o cualquier otro parámetro necesario para la solicitud
            // Por ejemplo, si necesitas configurar algún token o encabezado específico, hazlo aquí.

            webRequest.downloadHandler = new DownloadHandlerBuffer();  // Inicializa el DownloadHandler

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // Autenticación exitosa, realiza acciones adicionales según tus necesidades
                string response = webRequest.downloadHandler.text;

                // Verifica si la respuesta contiene la información esperada para determinar que la autenticación fue exitosa
                if (!string.IsNullOrEmpty(response))
                {
                    print(response);
                    // Autenticación exitosa, procede al siguiente paso de la aplicación
                    ProceedToNextStep();
                }
                else
                {
                    Debug.LogError("Respuesta no válida después de la autenticación.");
                }
            }
            else
            {
                // Error en la autenticación, maneja el error adecuadamente
                Debug.LogError("Error en la autenticación: " + webRequest.error);
            }
        }
    }

    // Método para verificar si la autenticación fue exitosa
    public bool IsAuthenticationSuccessful()
    {
        return isAuthenticationSuccessful;
    }

    public void ResetAuthenticationStatus()
    {
        isAuthenticationSuccessful = false;
    }
    private void ProceedToNextStep()
    {
        // Aquí puedes implementar el código para continuar con el siguiente paso de tu aplicación después de la autenticación exitosa.
        Debug.Log("Autenticación exitosa. Procediendo al siguiente paso...");
    }
}
