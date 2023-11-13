using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;
using System.Collections;

namespace Canvastique3D
{
    public class StreamingServer : MonoBehaviour
    {
        [SerializeField] private Camera monitorCamera;
        [SerializeField] private int listenPort = 8080; // This is the port the server will listen on

        private UdpClient udpListener;
        private IPEndPoint clientEndPoint; // This will be set when we receive a message

        private const int bufferSize = 65507;
        private const int HEADER_SIZE = 8;
        private int packetSize;

        private Texture2D reusableTexture;

        private bool isStreaming = false;

        private void Awake()
        {
            udpListener = new UdpClient(listenPort);
            packetSize = bufferSize - HEADER_SIZE;
            reusableTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
        }

        void Update()
        {
            if (isStreaming && clientEndPoint != null)
            {
                SendCompressedFrame();
            }
        }

        private IEnumerator FindClientIPCoroutine()
        {
            Debug.Log($"Server listening on port {listenPort}.");
            while (true)
            {
                if (udpListener.Available > 0)
                {
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpListener.Receive(ref anyIP);
                    string message = System.Text.Encoding.ASCII.GetString(data);

                    if (message == "DISCOVER_SERVER_REQUEST")
                    {
                        // Respond to discovery request with the server's IP address
                        string serverIPAddress = GetLocalIPAddress(); // Get the server's local IP address
                        byte[] response = System.Text.Encoding.ASCII.GetBytes("DISCOVER_SERVER_RESPONSE|" + serverIPAddress);
                        udpListener.Send(response, response.Length, anyIP);
                        clientEndPoint = anyIP; // Store the client's endpoint
                        EventManager.instance.TriggerConnected(clientEndPoint.ToString());
                        Debug.Log("Client discovered the server and connected.");
                    }
                    else
                    {
                        Debug.LogWarning("Received unexpected message: " + message);
                    }
                }
                yield return new WaitForSeconds(1.0f);
            }
        }

        private string GetLocalIPAddress()
        {
            // Get the local IP address of the server
            string ipAddress = "";
            foreach (IPAddress localIp in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (localIp.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = localIp.ToString();
                    break;
                }
            }
            return ipAddress;
        }

        private void SendCompressedFrame()
        {
            RenderTexture currentRT = RenderTexture.active;
            
            RenderTexture.active = monitorCamera.targetTexture;

            // Create a temporary RenderTexture with the desired resolution
            RenderTexture tempRT = RenderTexture.GetTemporary(512, 512);

            // Render the source texture into the temporary texture with downsampling
            Graphics.Blit(monitorCamera.targetTexture, tempRT, new Material(Shader.Find("Unlit/Texture")));

            // Read pixels from the temporary texture
            reusableTexture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);

            // Restore the previous active RenderTexture
            RenderTexture.active = currentRT;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tempRT);

            reusableTexture.Apply();

            byte[] jpegData = reusableTexture.EncodeToJPG();

            int totalPackets = (int)Math.Ceiling((double)jpegData.Length / packetSize);

            for (int i = 0; i < totalPackets; i++)
            {
                int offset = i * packetSize;
                int size = Math.Min(packetSize, jpegData.Length - offset);

                byte[] packetData = new byte[size + HEADER_SIZE];
                BitConverter.GetBytes(totalPackets).CopyTo(packetData, 0);
                BitConverter.GetBytes(i).CopyTo(packetData, 4);
                Array.Copy(jpegData, offset, packetData, HEADER_SIZE, size);

                udpListener.Send(packetData, packetData.Length, clientEndPoint);
            }
        }


        public void StartStreaming()
        {
            isStreaming = true;
        }

        public void StopStreaming()
        {
            isStreaming = false;
        }

        public void ConnectClient()
        {
            StartCoroutine(FindClientIPCoroutine());
        }

        public void DisconnectClient()
        {
            isStreaming = false;
            StopCoroutine(FindClientIPCoroutine());
            clientEndPoint = null;
        }

        public void Send3DFile(byte[] fileData)
        {
            // Logic to send 3D files to the connected client
            // You can define a protocol to handle file transfer
        }

        private void OnDestroy()
        {
            // Clean up
            if (reusableTexture != null)
            {
                Destroy(reusableTexture);
            }
            if (udpListener != null)
            {
                udpListener.Close();
            }
        }

        private void OnApplicationQuit()
        {
            if (udpListener != null)
            {
                udpListener.Close();
            }
        }
    }
}
