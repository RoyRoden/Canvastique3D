using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;

namespace Canvastique3D
{
    public class StreamingServer : MonoBehaviour
    {
        [SerializeField] private Camera monitorCamera;
        [SerializeField] private string clientIP = "127.0.0.1";
        [SerializeField] private int port = 8080;

        private IPEndPoint clientEndPoint;
        private UdpClient udpClient;

        private const int bufferSize = 65507;
        private const int HEADER_SIZE = 8; // 4 bytes for total packets + 4 bytes for current packet number
        private int packetSize;

        private Texture2D reusableTexture;

        private void Start()
        {
            udpClient = new UdpClient();
            clientEndPoint = new IPEndPoint(IPAddress.Parse(clientIP), port);
            packetSize = bufferSize - HEADER_SIZE;

            // Create a reusable texture
            reusableTexture = new Texture2D(monitorCamera.targetTexture.width, monitorCamera.targetTexture.height, TextureFormat.RGB24, false);

            Debug.Log($"Server started and ready to send frames to {clientIP}:{port}.");
        }

        void Update()
        {
            SendCompressedFrame();
        }

        private void SendCompressedFrame()
        {
            // Use the reusable texture
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = monitorCamera.targetTexture;
            monitorCamera.Render();

            reusableTexture.ReadPixels(new Rect(0, 0, monitorCamera.targetTexture.width, monitorCamera.targetTexture.height), 0, 0);
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

                udpClient.Send(packetData, packetData.Length, clientEndPoint);
            }

            RenderTexture.active = currentRT;
        }

        private void OnDestroy()
        {
            if (reusableTexture != null)
            {
                Destroy(reusableTexture);
            }
        }

        private void OnApplicationQuit()
        {
            udpClient.Close();
        }
    }
}
