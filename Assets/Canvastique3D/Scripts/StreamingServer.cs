using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using System.Collections;

namespace Canvastique3D
{
    public class StreamingServer : MonoBehaviour
    {
        [SerializeField] private Camera monitorCamera;

        [SerializeField] private int discoverServerPort = 12320;
        [SerializeField] private int discoverClientPort = 12324;
        [SerializeField] private int messagePort = 12325;
        [SerializeField] private int streamingPort = 12326;
        [SerializeField] private int teleportPort = 12327;

        private UdpClient udpDiscoverListener;
        private UdpClient udpResponseSender;
        private UdpClient udpMessageListener;
        private UdpClient udpStreamingListener;
        private UdpClient udpTeleportListener;

        private IPEndPoint receiveIPEndPoint = null;
        private IPEndPoint sendIPEndPoint = null;
        private IPEndPoint messageIPEndPoint = null;
        private IPEndPoint streamingIPEndPoint = null;
        private IPEndPoint teleportIPEndPoint = null;

        private const int bufferSize = 65507;
        private const int HEADER_SIZE = 8;
        private int packetSize;

        private Texture2D reusableTexture;
        private Material blitMaterial;

        private bool isIPDiscovered = false;

        private void Awake()
        {
            udpDiscoverListener = new UdpClient(discoverServerPort);
            udpResponseSender = new UdpClient();
            udpMessageListener = new UdpClient();
            udpStreamingListener = new UdpClient();
            udpTeleportListener = new UdpClient();

            packetSize = bufferSize - HEADER_SIZE;

            reusableTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            blitMaterial = new Material(Shader.Find("Unlit/Texture"));
        }


        #region CONNECT

        public void Connect()
        {
            StopAllCoroutines();
            StartCoroutine(DiscoverLocalIPCoroutine());
        }

        public void Disconnect()
        {
            StopCoroutine(DiscoverLocalIPCoroutine());
            messageIPEndPoint = null;
            streamingIPEndPoint = null;
            teleportIPEndPoint = null;

            isIPDiscovered = false;
        }

        private IEnumerator DiscoverLocalIPCoroutine()
        {
            Debug.Log($"Server listening on port {discoverServerPort}.");

            while (isIPDiscovered == false)
            {
                if (udpDiscoverListener.Available > 0)
                {
                    receiveIPEndPoint = new IPEndPoint(IPAddress.Any, discoverServerPort);
                    byte[] data = udpDiscoverListener.Receive(ref receiveIPEndPoint);
                    string message = System.Text.Encoding.ASCII.GetString(data);
                    Debug.Log($"Received message from: {receiveIPEndPoint.Address}");

                    if (message == "DISCOVER_SERVER_REQUEST")
                    {
                        string serverIPAddress = GetLocalIPAddress();
                        byte[] response = System.Text.Encoding.ASCII.GetBytes("DISCOVER_SERVER_RESPONSE|" + serverIPAddress);
                        Debug.Log($"Sent response: {"DISCOVER_SERVER_RESPONSE|" + serverIPAddress}");

                        sendIPEndPoint = new IPEndPoint(receiveIPEndPoint.Address, discoverClientPort);
                        // Send the response using the separate UdpClient for sending
                        udpResponseSender.Send(response, response.Length, sendIPEndPoint);

                        // Now you can set up different endpoints for different ports
                        messageIPEndPoint = new IPEndPoint(receiveIPEndPoint.Address, messagePort);
                        streamingIPEndPoint = new IPEndPoint(receiveIPEndPoint.Address, streamingPort);
                        teleportIPEndPoint = new IPEndPoint(receiveIPEndPoint.Address, teleportPort);
                        EventManager.instance.TriggerConnected(receiveIPEndPoint.Address.ToString());
                        Debug.Log("Client discovered the server and connected.");
                        isIPDiscovered = true;
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

        #endregion


        #region STREAMING

        public void StartStreaming()
        {
            DispatchMessage("START_STREAMING");
            StartCoroutine(CameraStreamingCoroutine());
        }

        public void StopStreaming()
        {
            StopCoroutine(CameraStreamingCoroutine());
        }

        private IEnumerator CameraStreamingCoroutine()
        {
            while(true)
            {
                RenderTexture currentRT = RenderTexture.active;

                RenderTexture.active = monitorCamera.targetTexture;

                // Create a temporary RenderTexture with the desired resolution
                RenderTexture tempRT = RenderTexture.GetTemporary(512, 512);

                Graphics.Blit(monitorCamera.targetTexture, tempRT, blitMaterial);

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

                    udpStreamingListener.Send(packetData, packetData.Length, streamingIPEndPoint);
                }
                
                RenderTexture.ReleaseTemporary(tempRT);

                yield return new WaitForSeconds(1.0f / 30.0f);
            }
        }

        #endregion


        #region TELEPORT

        public void Teleport(string modelFilePath)
        {
            DispatchMessage("TELEPORT");
            SendFile(modelFilePath);
        }

        public void SendFile(string filePath)
        {
            try
            {
                // Read the file as bytes
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Establish a TCP connection with the Android app
                TcpClient client = new TcpClient(receiveIPEndPoint.Address.ToString(), 12327); // Replace with your Android device's IP address

                // Get a stream for writing data to the client
                NetworkStream stream = client.GetStream();

                // Send the file bytes
                stream.Write(fileBytes, 0, fileBytes.Length);

                // Close the stream and the client
                stream.Close();
                client.Close();

                Debug.Log("File sent successfully.");
                EventManager.instance.TriggerTeleported();
            }
            catch (Exception e)
            {
                EventManager.instance.TriggerWarning($"Error sending file: {e.Message}");
                EventManager.instance.TriggerTeleported();
            }
        }

        public void SendMaterialName(string materialName)
        {
            DispatchMessage($"MATERIAL_NAME|"+materialName);
        }
    
        #endregion


        private void DispatchMessage(string msg)
        {
            if (messageIPEndPoint != null)
            {
                byte[] messageData = System.Text.Encoding.ASCII.GetBytes(msg);
                udpMessageListener.Send(messageData, messageData.Length, messageIPEndPoint);
                Debug.Log($"Sent message: {msg}");
            }
        }


        private void OnDestroy()
        {
            if (reusableTexture != null)
            {
                Destroy(reusableTexture);
            }
            if (udpStreamingListener != null)
            {
                udpStreamingListener.Close();
            }
            if (udpTeleportListener != null)
            {
                udpTeleportListener.Close();
            }
            if (blitMaterial != null)
            {
                Destroy(blitMaterial);
            }
        }

        private void OnApplicationQuit()
        {
            if (reusableTexture != null)
            {
                Destroy(reusableTexture);
            }
            if (udpStreamingListener != null)
            {
                udpStreamingListener.Close();
            }
            if (udpTeleportListener != null)
            {
                udpTeleportListener.Close();
            }
            if (blitMaterial != null)
            {
                Destroy(blitMaterial);
            }
        }
    }
}
