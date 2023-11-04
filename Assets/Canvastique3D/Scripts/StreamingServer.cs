using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System;
using System.Text;

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

        private void Start()
        {
            udpListener = new UdpClient(listenPort);
            packetSize = bufferSize - HEADER_SIZE;
            reusableTexture = new Texture2D(512, 512, TextureFormat.RGB24, false);
            Debug.Log($"Server listening on port {listenPort}.");
        }

        void Update()
        {
            CheckForDiscoveryRequests();

            if (clientEndPoint != null)
            {
                // We have a client connected, send the frame
                SendCompressedFrame();
            }
            else if (udpListener.Available > 0)
            {
                // We have data on the UDP client, this means we might have a new client
                ReceiveInitialClientMessage();
            }
        }

        private void CheckForDiscoveryRequests()
        {
            if (udpListener.Available > 0)
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.Receive(ref anyIP);
                string message = System.Text.Encoding.ASCII.GetString(data);

                if (message == "DISCOVER_SERVER_REQUEST")
                {
                    byte[] response = System.Text.Encoding.ASCII.GetBytes("DISCOVER_SERVER_RESPONSE");
                    udpListener.Send(response, response.Length, anyIP);
                    Debug.Log("Responded to discovery request.");
                }
            }
        }

        private void ReceiveInitialClientMessage()
        {
            // Receive the first message from any client
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpListener.Receive(ref anyIP);

            if (data.Length > 0)
            {
                string receivedText = Encoding.UTF8.GetString(data); // Ensure the encoding matches the client.
                Debug.Log($"Received message: {receivedText} from {anyIP}");

                // Check if the message is the expected "HELLO" message
                if (receivedText == "HELLO")
                {
                    clientEndPoint = anyIP; // Store the client's endpoint
                    SendHelloAckMessage(anyIP);
                }
                else
                {
                    Debug.LogWarning("Received unexpected message: " + receivedText);
                }
            }
        }

        private void SendHelloAckMessage(IPEndPoint clientIP)
        {
            string ackMessage = "HELLO_ACK";
            byte[] ackData = Encoding.UTF8.GetBytes(ackMessage);
            udpListener.Send(ackData, ackData.Length, clientIP);
            Debug.Log("Sent HELLO_ACK message to client.");
        }

        private void SendCompressedFrame()
        {
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

                udpListener.Send(packetData, packetData.Length, clientEndPoint);
            }
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
