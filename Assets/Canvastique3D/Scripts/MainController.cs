using UnityEngine;

namespace Canvastique3D
{
    // Main Controller for handling operations related to webcam, UI, perspective projection, and painting.
    public class MainController : MonoBehaviour
    {
        // Controller and Component references
        private RuntimeUI runtimeUI;
        private WebcamController webcamController;
        private MaterialController materialController;
        private PerspectiveProjection perspectiveProjection;
        private CanvasRecognitionDLL canvasRecognitionDLL;
        private ModelController modelController;
        private GalleryController galleryController;
        private StreamingServer streamingServer;

        // Flag to toggle canvas detection
        private bool isCanvasDetectionOn = false;

        // Awake is used to initialize any variables or game state before the game starts
        private void Awake()
        {
            // Fetch or add components
            runtimeUI = GetComponent<RuntimeUI>() ?? gameObject.AddComponent<RuntimeUI>();
            webcamController = GetComponent<WebcamController>() ?? gameObject.AddComponent<WebcamController>();
            materialController = GetComponent<MaterialController>() ?? gameObject.AddComponent<MaterialController>();
            perspectiveProjection = GetComponent<PerspectiveProjection>() ?? gameObject.AddComponent<PerspectiveProjection>();
            canvasRecognitionDLL = GetComponent<CanvasRecognitionDLL>() ?? gameObject.AddComponent<CanvasRecognitionDLL>();
            modelController = GetComponent<ModelController>() ?? gameObject.AddComponent<ModelController>();
            galleryController = GetComponent<GalleryController>() ?? gameObject.AddComponent<GalleryController>();
            streamingServer = GetComponent<StreamingServer>() ?? gameObject.AddComponent<StreamingServer>();

            // Subscribe to events
            EventManager.instance.OnStartCamera += StartCamera;
            EventManager.instance.OnStopCamera += StopCamera;
            EventManager.instance.OnStartCalibration += StartCalibration;
            EventManager.instance.OnStopCalibration += StopCalibration;
            EventManager.instance.OnFrame += Frame;
            EventManager.instance.OnLoadModel += LoadModel;
            EventManager.instance.OnLoadCapture += LoadCapture;
            EventManager.instance.OnCapture += Capture;
            EventManager.instance.OnRemoveCapture += RemoveCapture;
            EventManager.instance.OnVariation += Variation;
            EventManager.instance.OnLoadCamera += LoadCamera;
            EventManager.instance.OnResetMaterial += ResetMaterial;
            EventManager.instance.OnChangePosition += ChangePosition;
            EventManager.instance.OnChangeRotation += ChangeRotation;
            EventManager.instance.OnAssignMaterial += AssignMaterial;
            EventManager.instance.OnConnect += Connect;
            EventManager.instance.OnDisconnect += Disconnect;
            EventManager.instance.OnStartStreaming += StartStreaming;
            EventManager.instance.OnStopStreaming += StopStreaming;
            EventManager.instance.OnTeleport += Teleport;
        }

        // Start is called before the first frame update
        void Start()
        {
            galleryController.Init();
            materialController.Init();
            webcamController.Init();
        }

        // Update is called once per frame
        private void Update()
        {
            if (webcamController.IsPlaying())
            {
                var rawFrame = webcamController.RawFrame();

                if (isCanvasDetectionOn)
                {
                    canvasRecognitionDLL.PerformRecognition(ref rawFrame, webcamController.Width, webcamController.Height, runtimeUI.BlurKSize, runtimeUI.Threshold, runtimeUI.ThresholdMax, runtimeUI.EpsilonFactor, runtimeUI.AreaFactor, runtimeUI.Step);
                }

                materialController.ProcessedTexture.SetPixels32(rawFrame);
                materialController.ProcessedTexture.Apply();
            }
        }

        // Initializes and starts the camera with the selected device and parameters from the UI
        private void StartCamera()
        {
            var deviceName = runtimeUI.Webcam;
            var requestedResolution = runtimeUI.RequestedResolution;

            webcamController.InitializeWebcam(deviceName, requestedResolution, 30);
            webcamController.StartWebcam();

            materialController.InitializeProcessedTexture(webcamController.Resolution);
        }

        // Stops the camera and resets the material
        private void StopCamera()
        {
            webcamController.StopWebcam();
        }

        // Loads the current painting
        private void LoadCapture(Texture2D capture)
        {
            materialController.StopSpriteAnimation();
            materialController.ResetTransform();
            materialController.InitializeMaterial(capture);
        }

        private void LoadCamera()
        {
            if (!webcamController.IsPlaying()) StartCamera();

            materialController.LoadTransform();
            materialController.InitializeMaterial(materialController.ProcessedTexture);
        }

        private void StartCalibration()
        {
            StopCamera();

            LoadCamera();

            perspectiveProjection.InitializeMatrix();

            canvasRecognitionDLL.InitializePoints(webcamController.Width, webcamController.Height);
            Debug.Log($"Points initialized with width {webcamController.Width} and height {webcamController.Height}");

            ResetTransform();

            isCanvasDetectionOn = true;
            EventManager.instance.TriggerCalibrationStarted();
        }

        private void StopCalibration()
        {
            if (isCanvasDetectionOn)
            {
                isCanvasDetectionOn = false;
                EventManager.instance.TriggerCalibrationStopped();
            }
        }

        // Adjusts the perspective according to the points detected by CanvasRecognitionDLL
        private void Frame()
        {
            foreach (var point in canvasRecognitionDLL.Points)
            {
                Debug.Log(point);
            }

            materialController.SetTransform(perspectiveProjection.GetMatrix(canvasRecognitionDLL.Points, webcamController.Width, webcamController.Height));

            EventManager.instance.TriggerStopCalibration();
            EventManager.instance.TriggerFramed();
        }

        // Resets the transform and reinitializes the points and matrix
        private void ResetTransform()
        {
            materialController.ResetTransform();
            canvasRecognitionDLL.InitializePoints(webcamController.Width, webcamController.Height);
            perspectiveProjection.InitializeMatrix();
            Debug.Log("Transform Reset.");
        }

        private void LoadModel()
        {
            modelController.LoadModel();
        }

        private void ChangePosition(Vector3Int position)
        {
            modelController.ChangePosition(position);
        }

        private void ChangeRotation(Vector3Int rotation)
        {
            modelController.ChangeRotation(rotation);
        }

        private void Teleport()
        {
            streamingServer.Teleport(modelController.getModelFilePath());
        }

        private void Capture()
        {
            galleryController.Capture();
        }

        private void Variation(string APIKey)
        {
            materialController.PlaySpriteAnimation();
            galleryController.Variation(APIKey);
        }

        private void RemoveCapture(string fileName)
        {
            galleryController.RemoveCapture(fileName);
        }

        private void ResetMaterial()
        {
            materialController.ResetMaterial();
        }

        private void AssignMaterial(string materialName)
        {
            modelController.AssignMaterialByName(materialName);
            streamingServer.SendMaterialName(materialName);
        }

        private void Connect()
        {
            streamingServer.Connect();
        }

        private void Disconnect()
        {
            streamingServer.Disconnect();
        }

        private void StartStreaming()
        {
            streamingServer.StartStreaming();
        }

        private void StopStreaming()
        {
            streamingServer.StopStreaming();
        }
    }
}
