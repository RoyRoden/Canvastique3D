using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Canvastique3D
{
    // Data structure to hold a base64 JSON string.
    [Serializable]
    public class Data
    {
        public string b64_json;
    }

    // JSON response structure for the list of Data objects.
    [Serializable]
    public class JsonResponse
    {
        public List<Data> data;
    }

    public class GalleryController : MonoBehaviour
    {
        [SerializeField]
        private Camera monitorCamera;
        private string timeStamp = "yyMMddHHmmss";
        private string extension = ".png";
        private string dirPath;
        private int size;
        private Texture2D cameraTexture;

        // Called when the script instance is being loaded.
        private void Awake()
        {
            dirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Canvastique3D");
            Directory.CreateDirectory(dirPath);

            if (monitorCamera == null)
                Debug.LogError("Missing camera object");

            size = monitorCamera.targetTexture.width;
            cameraTexture = new Texture2D(size, size, TextureFormat.RGB24, false);
        }

        public void Init()
        {
            LoadCaptures();
        }

        private void LoadCaptures()
        {
            var captureFiles = Directory.GetFiles(dirPath)
                                .Where(file => file.EndsWith(extension))
                                .OrderBy(file => new FileInfo(file).CreationTime)
                                .ToArray();

            int index = 0;
            foreach (var file in captureFiles)
            {
                if (file.EndsWith(extension))
                {
                    var texture = new Texture2D(0, 0);
                    texture.LoadImage(File.ReadAllBytes(file));
                    var captureData = new CaptureData
                    {
                        Texture = texture,
                        FileName = Path.GetFileNameWithoutExtension(file)
                    };
                    EventManager.instance.TriggerCaptured(captureData);
                    index++;
                }
            }
        }

        public void Capture()
        {
            // Read the pixels from the camera's target texture into the painting texture.
            RenderTexture.active = monitorCamera.targetTexture;
            cameraTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            cameraTexture.Apply();
            RenderTexture.active = null;

            var file = Path.Combine(dirPath, "Capture" + DateTime.Now.ToString(timeStamp) + extension);
            var bytes = cameraTexture.EncodeToPNG();
            File.WriteAllBytes(file, bytes);
            Debug.Log($"Image decoded and saved to {file}");

            // Check if the file exists and has content
            if (File.Exists(file) && new FileInfo(file).Length > 0)
            {
                var captureData = new CaptureData
                {
                    Texture = cameraTexture,
                    FileName = Path.GetFileNameWithoutExtension(file)
                };
                EventManager.instance.TriggerCaptured(captureData);
            }
            else
            {
                EventManager.instance.TriggerError($"Failed to save the image to {file}");
            }
        }

        public void RemoveCapture(string fileName)
        {
            var fullPath = Path.Combine(dirPath, fileName + extension);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);

                EventManager.instance.TriggerCaptureRemoved(fileName);
            }
            else
            {
                EventManager.instance.TriggerWarning($"{fullPath} does not exist!");
            }
        }

        public void Variation(string APIKey)
        {
            StartCoroutine(GenerateVariationCoroutine(APIKey));
        }

        // Coroutine to make an asynchronous request to OpenAI's API.
        private IEnumerator GenerateVariationCoroutine(string APIKey)
        {
            // The URI of OpenAI's API endpoint.
            var uri = "https://api.openai.com/v1/images/variations";

            byte[] fileData;

            // Read the pixels from the camera's target texture into the painting texture.
            RenderTexture.active = monitorCamera.targetTexture;
            cameraTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            //cameraTexture.Apply();
            RenderTexture.active = null;

            fileData = cameraTexture.EncodeToPNG();

            var _APIKey = APIKey;

            // Create a new WWWForm and add the image data and other fields to it.
            WWWForm form = new WWWForm();
            form.AddBinaryData("image", fileData);
            form.AddField("n", 1);
            form.AddField("size", "1024x1024");
            form.AddField("response_format", "b64_json");

            // Make a POST request to the OpenAI API.
            using (UnityWebRequest www = UnityWebRequest.Post(uri, form))
            {
                // Set the Authorization header to "Bearer {apiKey}".
                www.SetRequestHeader("Authorization", "Bearer " + _APIKey);
                www.timeout = 20;
                yield return www.SendWebRequest();

                // Log an error message if the request failed.
                if (www.result != UnityWebRequest.Result.Success)
                {
                    EventManager.instance.TriggerVariationError(www.error);
                }
                else
                {
                    // Log a success message if the request succeeded.
                    Debug.Log("OpenAI request complete!");

                    // Parse the JSON response.
                    JsonResponse jsonResponse = JsonUtility.FromJson<JsonResponse>(www.downloadHandler.text);

                    // Get the base64 image string from the first item in the "data" array of the JSON response.
                    var base64Image = jsonResponse.data[0].b64_json;

                    // Convert the base64 string to a byte array.
                    var imageBytes = Convert.FromBase64String(base64Image);

                    // Create a texture2D object from the byte array.
                    var texture = new Texture2D(2, 2);
                    texture.LoadImage(imageBytes);  // Automatically resizes the texture dimensions.

                    var file = Path.Combine(dirPath, "Variation" + DateTime.Now.ToString(timeStamp) + extension);
                    var bytes = texture.EncodeToPNG();

                    File.WriteAllBytes(file, bytes);
                    Debug.Log($"Image decoded and saved to {file}");

                    var captureData = new CaptureData
                    {
                        Texture = texture,
                        FileName = Path.GetFileNameWithoutExtension(file)
                    };
                    EventManager.instance.TriggerCaptured(captureData);
                }
            }
        }
    }
}
