using UnityEngine;
using Microsoft.WindowsAzure.MobileServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
public class AuthenticationManager : MonoBehaviour
{
   private MobileServiceClient mobileServiceClient;
   public async void StartAuth()
   {
       // Inicializa el cliente de Mobile Services
       mobileServiceClient = new MobileServiceClient("https://asktai.azurewebsites.net");
       // Autentica al usuario
       await Authenticate();
   }
   private async Task Authenticate()
    {
        while (mobileServiceClient.CurrentUser == null)  // Usa la instancia mobileServiceClient
        {
            try
            {
                // Inicia el flujo de autenticación con Google
                //await mobileServiceClient.LoginAsync(MobileServiceAuthenticationProvider.Google, JObject.Parse(token));
                Debug.Log("Autenticación exitosa");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error de autenticación: {ex.Message}");
            }
        }
    }
}