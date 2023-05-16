
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class AWS3 : MonoBehaviour
{
private const string awsBucketName = "utranscription";
private const string awsAccessKey = "AKIAZQK7KMYL3D5OYUEN"; //accessKeyId: 'AKIAZQK7KMYL3D5OYUEN',
private const string awsSecretKey = "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"; //secretAccessKey: 'XVE9AY1OkQK6jiIeQ1zvPrZEcx7XNekrQgOnvKi2',
private string awsURLBaseVirtual = ""; //region: 'us-east-2',
private const string FileName = "welcome.mp3";
private const string FilePath = "Assets/welcome.mp3";

void Start()
{
        System.Net.ServicePointManager.Expect100Continue = false;
        awsURLBaseVirtual = "https://" + awsBucketName + ".s3-us-east-2.amazonaws.com/";
} 


/* public void Conectarse()

{
        awsURLBaseVirtual = "https://" + awsBucketName + ".s3-us-east-2.amazonaws.com/";
} */

public void UploadFileToAWS3()

{
        string currentAWS3Date = 
        System.DateTime.UtcNow.ToString(
        "ddd, dd MMM yyyy HH:mm:ss ") + "GMT";

        string canonicalString =
        "PUT\n\n\n\nx-amz-date:" + currentAWS3Date + "\n/" + awsBucketName + "/" + "welcome.mp3"; UTF8Encoding encode = new UTF8Encoding();
        HMACSHA1 signature = new HMACSHA1();
        signature.Key = encode.GetBytes(awsSecretKey);
        byte[] bytes = encode.GetBytes(canonicalString);
        byte[] moreBytes = signature.ComputeHash(bytes);
        string encodedCanonical = Convert.ToBase64String(moreBytes); string aws3Header = "AWS " + awsAccessKey + ":" + encodedCanonical; string URL3 = awsURLBaseVirtual + FileName; WebRequest requestS3 = 
        (HttpWebRequest)WebRequest.Create(URL3);

        requestS3.Headers.Add("Authorization", aws3Header);

        requestS3.Headers.Add("x-amz-date", currentAWS3Date); byte[] fileRawBytes = File.ReadAllBytes("Assets/welcome.mp3");

        requestS3.ContentLength = fileRawBytes.Length; requestS3.Method = "PUT"; Stream S3Stream = requestS3.GetRequestStream();

        S3Stream.Write(fileRawBytes, 0, fileRawBytes.Length);

        Debug.Log("Sent bytes: " + requestS3.ContentLength + ", for file: " + FileName); 

        S3Stream.Close();

        Debug.Log("Archivo enviado exitosamente.");
}
} 