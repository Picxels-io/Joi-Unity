/* using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

public class Conect : MonoBehaviour
{
    private const string awsBucketName = "utranscription";
    private const string bucketName = "utranscription";
    private const string awsAccessKey = "AKIAZQK7KMYL3D5OYUEN"; //accessKeyId: 'AKIAZQK7KMYL3D5OYUEN',
    private const string awsSecretKey = "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"; //secretAccessKey: 'XVE9AY1OkQK6jiIeQ1zvPrZEcx7XNekrQgOnvKi2',
    private string awsURLBaseVirtual = ""; //region: 'us-east-2',
    private string filePath = "Assets/welcome.mp3";
    private string fileName = "welcome.mp3";

    void Start()
    {
        // Configura las credenciales de acceso
        var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);

        // Configura la región
        var region = RegionEndpoint.USEast2;

        // Crea una instancia del cliente de Amazon S3
        var client = new AmazonS3Client(credentials, region);

        // Crea la solicitud de carga
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = fileName,
            FilePath = filePath
        };

        // Crea una tarea para completar la operación
        var tcs = new TaskCompletionSource<bool>();

        // Envía la solicitud de carga
        client.PutObjectAsync(request, (responseObj) =>
        {
            if (responseObj.Exception == null)
            {
                Debug.Log("Archivo de audio enviado exitosamente.");
                tcs.SetResult(true);
            }
            else
            {
                Debug.Log("Error al enviar el archivo de audio: " + responseObj.Exception);
                tcs.SetException(responseObj.Exception);
            }
        });

        // Espera a que la tarea se complete
        tcs.Task.Wait();
    }
} */