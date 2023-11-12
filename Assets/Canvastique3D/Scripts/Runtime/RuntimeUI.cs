using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;

namespace Canvastique3D
{
    // Manages the runtime user interface and its interactions
    public class RuntimeUI : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActionAsset;

        private VisualElement root;

        private Label settingsTip;
        private VisualElement settingsContainer;
        private VisualElement capturesContainer;
        private VisualElement galleryContainer;
        private VisualElement modelContainer;

        private VisualElement cameraPreview;

        private Button calibrationButton;
        private Button frameButton;
        private Button loadButton;
        private Button captureButton;
        private Button variationButton;

        private Button cameraSwitch;

        private Label modelNameLabel;
        private DropdownField materialDropdown;

        private DropdownField webcamDropdown;
        private DropdownField resolutionDropdown;

        private Vector3IntField modelPositionOffset;
        private Vector3IntField modelRotationOffset;

        private VisualElement messageBox;
        private Button messageConfirm;

        private VisualElement deleteModal;
        private Button deleteConfirm;
        private Button deleteCancel;

        private VisualElement keyModal;
        private Button keyConfirm;
        private Button keyCancel;
        private TextField keyTextField;
        private Toggle keyRememberToggle;

        private Button connectButton;
        private Button streamButton;
        private Button transferButton;
        private Label streamingStatus;

        private const string THRESHOLD_KEY = "ThresholdValue";
        private const string API_KEY = "APIKeyValue";

        private void Awake()
        {
            root = GetComponent<UIDocument>().rootVisualElement;

            settingsTip = root.Q<Label>("SettingsTip");
            settingsTip.style.display = DisplayStyle.None;

            settingsContainer = root.Q<VisualElement>("SettingsContainer");
            settingsContainer.style.display = DisplayStyle.None;
            settingsContainer.RegisterCallback<PointerOverEvent>(OnPointerOver);
            settingsContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            galleryContainer = root.Q<VisualElement>("GalleryScrollView");
            galleryContainer.RegisterCallback<PointerOverEvent>(OnPointerOver);
            galleryContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            capturesContainer = root.Q<VisualElement>("CapturesContainer");
            EventManager.instance.OnCaptured += HandleCaptured;
            EventManager.instance.OnCaptureRemoved += HandleCaptureRemoved;

            cameraPreview = root.Q<VisualElement>("CameraPreview");
            cameraPreview.RegisterCallback<ClickEvent>(evt =>
            {
                DeselectAllCaptures();
                cameraPreview.AddToClassList("runtimeui-gallery-capture__selected");
                EventManager.instance.TriggerLoadCamera();
                variationButton.SetEnabled(false); 
                captureButton.SetEnabled(true);
                EventManager.instance.TriggerStopCalibration();
            });
            cameraSwitch = root.Q<Button>("CameraSwitch");
            cameraSwitch.RegisterCallback<ClickEvent>(evt =>
            {
                EventManager.instance.TriggerStopCamera();
            });

            EventManager.instance.OnCameraStopped += HandleCameraStopped;

            calibrationButton = root.Q<Button>("Calibration");
            calibrationButton.clicked += EventManager.instance.TriggerStartCalibration;
            EventManager.instance.OnCalibrationStarted += HandleCalibrationStarted;

            frameButton = root.Q<Button>("Frame");
            frameButton.clicked += EventManager.instance.TriggerFrame;
            EventManager.instance.OnCalibrationStopped += HandleCalibrationStopped;
            EventManager.instance.OnFramed += HandleFramed;

            modelContainer = root.Q<VisualElement>("ModelContainer");
            modelContainer.RegisterCallback<PointerOverEvent>(OnPointerOver);
            modelContainer.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            materialDropdown = root.Q<DropdownField>("Material");

            loadButton = root.Q<Button>("LoadModel");

            loadButton.RegisterCallback<ClickEvent>(evt =>
            {
                EventManager.instance.TriggerStopCalibration();
                EventManager.instance.TriggerLoadModel();
            });

            variationButton = root.Q<Button>("Variation");
            variationButton.clicked += OnVariationButtonClicked;
            variationButton.SetEnabled(false);
            EventManager.instance.OnVariationError += HandleVariationError;

            modelNameLabel = root.Q<Label>("ModelName");
            EventManager.instance.OnModelLoaded += HandleModelLoaded;

            webcamDropdown = root.Q<DropdownField>("Webcam");
            resolutionDropdown = root.Q<DropdownField>("Resolution");
            EventManager.instance.OnCameraNames += HandleCameraNames;

            modelPositionOffset = root.Q<Vector3IntField>("ModelPositionOffset");
            modelPositionOffset.RegisterValueChangedCallback<Vector3Int>(evt => 
            { 
                EventManager.instance.TriggerChangePosition(evt.newValue);
            });
            modelRotationOffset = root.Q<Vector3IntField>("ModelRotationOffset");
            modelRotationOffset.RegisterValueChangedCallback<Vector3Int>(evt =>
            {
                EventManager.instance.TriggerChangeRotation(evt.newValue);
            });

            captureButton = root.Q<Button>("Capture");
            captureButton.clicked += OnCaptureButtonClicked;
            captureButton.SetEnabled(false);

            messageBox = root.Q<VisualElement>("MessageBox");
            messageBox.style.display = DisplayStyle.None;
            messageConfirm = root.Q<Button>("MessageConfirm");
            EventManager.instance.OnError += HandleError;
            EventManager.instance.OnWarning += HandleWarning;

            deleteModal = root.Q<VisualElement>("DeleteModal");
            deleteConfirm = root.Q<Button>("DeleteConfirm");
            deleteCancel = root.Q<Button>("DeleteCancel");

            keyModal = root.Q<VisualElement>("KeyModal");
            keyConfirm = root.Q<Button>("KeyConfirm");
            keyCancel = root.Q<Button>("KeyCancel");
            keyTextField = root.Q<TextField>("KeyTextField");
            keyRememberToggle = root.Q<Toggle>("KeyRememberToggle");

            streamingStatus = root.Q<Label>("StreamingStatus");
            connectButton = root.Q<Button>("Connect");
            connectButton.clicked += OnConnectButtonClicked;
            EventManager.instance.OnConnected += HandleConnected;
            streamButton = root.Q<Button>("Stream");
            streamButton.SetEnabled(false);
            transferButton = root.Q<Button>("Transfer");
            transferButton.SetEnabled(false);

            LoadThresholdValue();
        }


        #region GETTERS
        // GETTERS
        public string Webcam => webcamDropdown.value;
        public string RequestedResolution => resolutionDropdown.value;
        public int Step = 4;
        public int BlurKSize = 1;
        public int Threshold => root.Q<SliderInt>("Threshold").value;
        public int ThresholdMax = 255;
        public float EpsilonFactor = 0.1f;
        public float AreaFactor = .25f;
        private string APIKey = "";
        #endregion


        #region CLICK PROCESSORS
        // CLICK PROCESSORS
        private void OnCaptureButtonClicked()
        {
            captureButton.SetEnabled(false);
            EventManager.instance.TriggerCapture();
        }

        private void OnVariationButtonClicked()
        {
            variationButton.SetEnabled(false);
            LoadAPIKey();
            StartCoroutine(KeyConfirmation());
        }

        private void OnConnectButtonClicked()
        {
            EventManager.instance.TriggerConnect();
            connectButton.clicked -= OnConnectButtonClicked;
            connectButton.clicked += OnDisconnectButtonClicked;
            connectButton.text = "Disconnect";
            streamingStatus.style.color = Color.yellow;
            streamingStatus.text = "Connecting...";
        }

        private void OnDisconnectButtonClicked()
        {
            EventManager.instance.TriggerDisconnect();
            connectButton.clicked += OnConnectButtonClicked;
            connectButton.clicked -= OnDisconnectButtonClicked;
            connectButton.text = "Connect";
            streamingStatus.style.color = Color.red;
            streamingStatus.text = "Disconnected";
        }

        IEnumerator KeyConfirmation()
        {
            // Show the key modal
            keyModal.style.display = DisplayStyle.Flex;
            keyTextField.value = APIKey;

            // Wait until either Confirm or Cancel is clicked
            bool? decision = null;
            keyConfirm.clicked += () => { decision = true; };
            keyCancel.clicked += () => { decision = false; };

            yield return new WaitUntil(() => decision.HasValue);

            if (keyRememberToggle.value) { 
                SaveAPIKey(keyTextField.value); 
            }

            if (decision.Value && keyTextField.value != "")
            {
                keyModal.style.display = DisplayStyle.None;
                EventManager.instance.TriggerVariation(keyTextField.value);
            } 
            else if (decision.Value && keyTextField.value == "")
            {
                keyModal.style.display = DisplayStyle.None;
                variationButton.SetEnabled(true);
                EventManager.instance.TriggerError("Key value must not be empty!");
            }
            else
            {
                variationButton.SetEnabled(true);
                keyModal.style.display = DisplayStyle.None;
            }
        }

        private void DeselectAllCaptures()
        {
            foreach (var child in capturesContainer.Children())
            {
                child.RemoveFromClassList("runtimeui-gallery-capture__selected");
            }
        }

        private string GetSelectedCaptureName()
        {
            foreach (var child in capturesContainer.Children())
            {
                if (child.ClassListContains("runtimeui-gallery-capture__selected"))
                {
                    return child.name;
                }
            }
            return null;
        }

        private VisualElement GetLastCapture()
        {
            List<VisualElement> capturePreviews = new List<VisualElement>();

            foreach (var child in capturesContainer.Children())
            {
                if (child.ClassListContains("runtimeui-gallery-capture"))
                {
                    capturePreviews.Add(child);
                }
            }

            // Check if there are at least two elements and return the background image of the second one.
            if (capturePreviews.Count >= 2)
            {
                return capturePreviews[1];
            }

            return null;
        }
        #endregion


        #region HANDLERS
        // HANDLERS
        private void HandleCameraNames(List<string> cameraNames)
        {
            webcamDropdown.choices = cameraNames;
            webcamDropdown.index = 0;
            webcamDropdown.RegisterValueChangedCallback(evt => EventManager.instance.TriggerStartCalibration());
            resolutionDropdown.RegisterValueChangedCallback(evt => EventManager.instance.TriggerStartCalibration());
        }

        private void HandleCalibrationStarted()
        {
            calibrationButton.style.display = DisplayStyle.None;
            settingsTip.style.display = DisplayStyle.Flex;
            settingsContainer.style.display = DisplayStyle.Flex;
            DeselectAllCaptures();
            cameraPreview.AddToClassList("runtimeui-gallery-capture__selected");
            variationButton.SetEnabled(false);
            captureButton.SetEnabled(false);
        }

        private void HandleCalibrationStopped()
        {
            calibrationButton.style.display = DisplayStyle.Flex;
            settingsTip.style.display = DisplayStyle.None;
            settingsContainer.style.display = DisplayStyle.None;
            SaveThresholdValue();
        }

        private void HandleFramed()
        {
            captureButton.SetEnabled(true);
        }

        private void HandleCameraStopped()
        {
            calibrationButton.style.display = DisplayStyle.Flex;
            settingsTip.style.display = DisplayStyle.None;
            settingsContainer.style.display = DisplayStyle.None;

            if (GetLastCapture() != null)
            {
                DeselectAllCaptures();
                var lastCapture = GetLastCapture();
                StyleBackground styleBackground = lastCapture.style.backgroundImage;
                EventManager.instance.TriggerLoadCapture(styleBackground.value.texture);
                lastCapture.AddToClassList("runtimeui-gallery-capture__selected");
                variationButton.SetEnabled(true);
                captureButton.SetEnabled(false);
            }
            else
            {
                DeselectAllCaptures();
                EventManager.instance.TriggerResetMaterial();
                variationButton.SetEnabled(false);
                captureButton.SetEnabled(false);
            }
        }

        private void HandleModelLoaded(string modelName, List<string> materialNames)
        {
            modelNameLabel.text = modelName;
            if(materialNames != null)
            {
                materialDropdown.choices.Clear();
                materialDropdown.index = -1;
                materialDropdown.choices = materialNames;
                materialDropdown.RegisterValueChangedCallback(evt =>
                {
                    EventManager.instance.TriggerAssignMaterial(materialDropdown.text);
                });
                EventManager.instance.TriggerAssignMaterial(materialNames[0]);
                materialDropdown.index = 0;
            }
            else
            {
                materialDropdown.choices.Clear();
                materialDropdown.index = -1;
                HandleWarning("No materials found!");
            }
        }

        private void HandleCaptured(CaptureData captureData)
        {
            var capturePreview = new VisualElement();
            capturePreview.AddToClassList("runtimeui-gallery-capture");
            capturePreview.name = captureData.FileName;
            capturePreview.style.backgroundImage = captureData.Texture;

            if (capturePreview.name.StartsWith("Variation"))
            {
                // Create an icon visual element
                var variationIcon = new VisualElement();
                variationIcon.AddToClassList("runtimeui-gallery-variation-icon");
                // Add the icon to the capture preview
                capturePreview.Add(variationIcon);
            }

            capturePreview.RegisterCallback<ClickEvent>(evt =>
            {
                DeselectAllCaptures();
                capturePreview.AddToClassList("runtimeui-gallery-capture__selected");
                EventManager.instance.TriggerLoadCapture(captureData.Texture);
                variationButton.SetEnabled(true);
                captureButton.SetEnabled(false);
                EventManager.instance.TriggerStopCalibration();
            });

            // Add the delete button
            var deleteButton = new Button(() =>
            {
                StartCoroutine(DeleteConfirmation(captureData.FileName));
            });
            deleteButton.AddToClassList("runtimeui-gallery-capture-remove");

            capturePreview.Add(deleteButton);

            capturesContainer.Insert(1, capturePreview);

            DeselectAllCaptures();
            capturePreview.AddToClassList("runtimeui-gallery-capture__selected");
            EventManager.instance.TriggerLoadCapture(captureData.Texture);
            variationButton.SetEnabled(true);
        }

        IEnumerator DeleteConfirmation(string fileName)
        {
            // Show the confirmation modal
            deleteModal.style.display = DisplayStyle.Flex;

            // Wait until either Confirm or Cancel is clicked
            bool? decision = null;
            deleteConfirm.clicked += () => { decision = true; };
            deleteCancel.clicked += () => { decision = false; };

            yield return new WaitUntil(() => decision.HasValue);

            if (decision.Value)
            {
                EventManager.instance.TriggerRemoveCapture(fileName);
            }

            deleteModal.style.display = DisplayStyle.None;

        }

        private void HandleCaptureRemoved(string fileName)
        {
            var capturePreview = capturesContainer.Q(name: fileName);

            if (capturePreview != null)
            {
                capturesContainer.Remove(capturePreview);
            }

            if(GetLastCapture() != null)
            {
                DeselectAllCaptures();
                var lastCapture = GetLastCapture();
                StyleBackground styleBackground = lastCapture.style.backgroundImage;
                EventManager.instance.TriggerLoadCapture(styleBackground.value.texture);
                lastCapture.AddToClassList("runtimeui-gallery-capture__selected");
                variationButton.SetEnabled(true);
                captureButton.SetEnabled(false);
            }
            else
            {
                DeselectAllCaptures();
                EventManager.instance.TriggerResetMaterial();
                variationButton.SetEnabled(false);
            }
        }

        private void HandleVariationError(string message)
        {
            HandleError(message);
            DeselectAllCaptures();
            var lastCapture = GetLastCapture();
            StyleBackground styleBackground = lastCapture.style.backgroundImage;
            EventManager.instance.TriggerLoadCapture(styleBackground.value.texture);
            lastCapture.AddToClassList("runtimeui-gallery-capture__selected");
            variationButton.SetEnabled(true);
            captureButton.SetEnabled(false);
        }

        private void HandleConnected(string clientIP)
        {
            streamingStatus.style.color = Color.green;
            streamingStatus.text = $"Connected to {clientIP}";
            streamButton.SetEnabled(true);
            transferButton.SetEnabled(true);
        }
        #endregion


        #region SLIDER INPUT HANDLING
        // SLIDER INPUT HANDLING
        private void OnPointerOver(PointerOverEvent evt)
        {
            inputActionAsset.Disable();
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            inputActionAsset.Enable();
        }
        #endregion


        #region MESSAGE HANDLING
        // ERROR AND WARNING MESSAGE HANDLING
        private void HandleError(string errorMessage)
        {
            DisplayMessage($"Error: \n{errorMessage}");
        }

        private void HandleWarning(string warningMessage)
        {
            DisplayMessage($"Warning: \n{warningMessage}");
        }

        private void DisplayMessage(string message)
        {
            messageBox.Q<Label>("MessageText").text = message;
            messageBox.style.display = DisplayStyle.Flex;
            messageConfirm.clicked += () => { messageBox.style.display = DisplayStyle.None; };
        }
        #endregion


        #region PLAYERPREFS
        // PLAYERPREFS
        private void LoadThresholdValue()
        {
            if (PlayerPrefs.HasKey(THRESHOLD_KEY))
            {
                int thresholdValue = PlayerPrefs.GetInt(THRESHOLD_KEY);
                root.Q<SliderInt>("Threshold").value = thresholdValue;
            }
        }

        private void SaveThresholdValue()
        {
            int thresholdValue = Threshold;
            PlayerPrefs.SetInt(THRESHOLD_KEY, thresholdValue);
            PlayerPrefs.Save();
        }

        private void LoadAPIKey()
        {
            if (PlayerPrefs.HasKey(API_KEY))
            {
                APIKey = PlayerPrefs.GetString(API_KEY);
            }
        }

        private void SaveAPIKey(string newKey)
        {
            PlayerPrefs.SetString(API_KEY, newKey);
            PlayerPrefs.Save();
        }
        #endregion
    }
}
