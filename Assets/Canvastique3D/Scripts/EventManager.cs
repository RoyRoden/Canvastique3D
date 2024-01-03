using System;
using System.Collections.Generic;
using UnityEngine;

namespace Canvastique3D
{
    public class EventManager
    {
        private static EventManager _instance;
        private static readonly object _lock = new object();

        public static EventManager instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new EventManager();
                    }
                    return _instance;
                }
            }
        }


        #region CAMERA
        // CAMERA
        public event Action OnStartCamera;
        public event Action OnCameraStarted;
        public event Action OnStopCamera;
        public event Action OnCameraStopped;
        public event Action<List<string>> OnCameraNames;

        public void TriggerStartCamera()
        {
            OnStartCamera?.Invoke();
        }

        public void TriggerCameraStarted()
        {
            OnCameraStarted?.Invoke();
        }

        public void TriggerStopCamera()
        {
            OnStopCamera?.Invoke();
        }

        public void TriggerCameraStopped()
        {
            OnCameraStopped?.Invoke();
        }

        public void TriggerCameraNames(List<string> cameraNames)
        {
            OnCameraNames?.Invoke(cameraNames);
        }
        #endregion


        #region CALIBRATION
        // CALIBRARION
        public event Action OnStartCalibration;
        public event Action OnCalibrationStarted;
        public event Action OnStopCalibration;
        public event Action OnCalibrationStopped;

        public void TriggerStartCalibration()
        {
            OnStartCalibration?.Invoke();
        }

        public void TriggerCalibrationStarted()
        {
            OnCalibrationStarted?.Invoke();
        }

        public void TriggerStopCalibration()
        {
            OnStopCalibration?.Invoke();
        }

        public void TriggerCalibrationStopped()
        {
            OnCalibrationStopped?.Invoke();
        }
        #endregion


        #region CAPTURE
        // CAPTURE
        public event Action OnCapture;
        public event Action<CaptureData> OnCaptured;
        public event Action<string> OnVariation;
        public event Action<Texture2D> OnLoadCapture;
        public event Action OnLoadCamera;
        public event Action<string> OnRemoveCapture;
        public event Action<string> OnCaptureRemoved;
        public event Action OnResetMaterial;
        public event Action<string> OnVariationError;

        public void TriggerCapture()
        {
            OnCapture?.Invoke();
        }

        public void TriggerCaptured(CaptureData captureData)
        {
            OnCaptured?.Invoke(captureData);
        }

        public void TriggerRemoveCapture(string fileName)
        {
            OnRemoveCapture?.Invoke(fileName);
        }

        public void TriggerCaptureRemoved(string fileName)
        {
            OnCaptureRemoved?.Invoke(fileName);
        }

        public void TriggerVariation(string APIKey)
        {
            OnVariation?.Invoke(APIKey);
        }

        public void TriggerVariationError(string message)
        {
            OnVariationError?.Invoke(message);
        }

        public void TriggerLoadCapture(Texture2D capture)
        {
            OnLoadCapture?.Invoke(capture);
        }

        public void TriggerLoadCamera()
        {
            OnLoadCamera?.Invoke();
        }

        public void TriggerResetMaterial()
        {
            OnResetMaterial?.Invoke();
        }
        #endregion


        #region MODEL
        // MODEL
        public event Action OnLoadModel;
        public event Action<string, List<string>> OnModelLoaded;
        public event Action<Vector3Int> OnChangePosition;
        public event Action<Vector3Int> OnChangeRotation;
        public event Action<string> OnAssignMaterial;

        public void TriggerLoadModel()
        {
            OnLoadModel?.Invoke();
        }

        public void TriggerModelLoaded(string modelName, List<string> materialNames)
        {
            OnModelLoaded?.Invoke(modelName, materialNames);
        }

        public void TriggerChangePosition(Vector3Int position)
        {
            OnChangePosition?.Invoke(position);
        }

        public void TriggerChangeRotation(Vector3Int rotation)
        {
            OnChangeRotation?.Invoke(rotation);
        }

        public void TriggerAssignMaterial(string materialName)
        {
            OnAssignMaterial?.Invoke(materialName);
        }
        #endregion


        #region FRAME
        // FRAME
        public event Action OnFrame;
        public event Action OnFramed;

        public void TriggerFrame()
        {
            OnFrame?.Invoke();
        }

        public void TriggerFramed()
        {
            OnFramed?.Invoke();
        }
        #endregion


        #region ERRORS AND WARNINGS
        // ERRORS AND WARNINGS
        public event Action<string> OnError;
        public event Action<string> OnWarning;

        public void TriggerError(string errorMessage)
        {
            OnError?.Invoke(errorMessage);
        }

        public void TriggerWarning(string warningMessage)
        {
            OnWarning?.Invoke(warningMessage);
        }
        #endregion

        #region STREAMING
        // STREAMING
        public event Action OnConnect;
        public event Action<string> OnConnected;
        public event Action OnDisconnect;
        public event Action OnDisconnected;
        public event Action OnStream;
        public event Action OnStopStream;
        public event Action<string> OnTransfer;
        public event Action OnTransferComplete;

        public void TriggerConnect()
        {
            OnConnect?.Invoke();
        }

        public void TriggerConnected(string clientIP)
        {
            OnConnected?.Invoke(clientIP);
        }

        public void TriggerDisconnect()
        {
            OnDisconnect?.Invoke();
        }

        public void TriggerDisconnected()
        {
            OnDisconnected?.Invoke();
        }

        public void TriggerStream()
        {
            OnStream?.Invoke();
        }

        public void TriggerStopStream()
        {
            OnStopStream?.Invoke();
        }

        public void TriggerTransfer(string modelName)
        {
            OnTransfer?.Invoke(modelName);
        }

        public void TriggerTransferCoplete()
        {
            OnTransferComplete?.Invoke();
        }
        #endregion
    }
}

