using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public class TaiRequest : MonoBehaviour
{
    private const string apiUrl = "https://asktaiprod.azurewebsites.net/conversations";
    public void StartRequest(List<string> conversationHistory, List<string> userInput)
    {
        StartCoroutine(MakeRequest(conversationHistory, userInput));
    }

    IEnumerator MakeRequest(List<string> conversationHistory, List<string> userInput)
    {
        // Datos que deseas enviar
        Dictionary<string, object> requestData = new Dictionary<string, object>
        {
            { "conversation_history", conversationHistory },
            { "user_input", userInput }
        };

        // Convertir datos a formato JSON
        string jsonData = JsonUtility.ToJson(requestData);

        // Configurar la solicitud HTTP POST
        using (HttpClient client = new HttpClient())
        {
            // Configurar encabezados
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Configurar el contenido
            HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Realizar la solicitud POST
            var task = client.PostAsync(apiUrl, content);

            // Esperar a que el Task se complete
            yield return new WaitUntil(() => task.IsCompleted);

            // Manejar la respuesta usando ContinueWith
            task.ContinueWith(t =>
            {
                if (t.Result.IsSuccessStatusCode)
                {
                    t.Result.Content.ReadAsStringAsync().ContinueWith(responseTask =>
                    {
                        string responseContent = responseTask.Result;
                        Debug.Log("Respuesta del servidor: " + responseContent);
                    }, TaskScheduler.FromCurrentSynchronizationContext()); // Esto asegura que el resultado se maneje en el hilo principal
                }
                else
                {
                    Debug.LogError("Error en la solicitud: " + t.Result.StatusCode);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}