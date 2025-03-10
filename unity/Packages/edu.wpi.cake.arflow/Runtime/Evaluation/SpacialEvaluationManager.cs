using UnityEngine;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

using static CakeLab.ARFlow.Utilities.MathUtilities;

namespace CakeLab.ARFlow.Evaluation
{
    using System.Collections.Generic;
    using OpenCVForUnityExample;
    using TMPro;
    using UnityEngine.UI;

    public class SpacialEvaluationManager : MonoBehaviour
    {

        // The textured plane, child to the arucoObj. 
        // With the way Unity sets up the gameobject texture, the plane is rotated 180 degree so that it matches the original orientation
        public GameObject arucoPlane;

        public Camera activeCamera;

        public TMP_Text infoText;
        public Button toggleButton;
        public TMP_Text toggleButtonText;

        public Button switchButton;
        public TMP_Text switchButtonText;

        public TMP_InputField iterationsInput;
        
        ArUcoSpacialEvaluation spacialEvaluation = new ArUcoSpacialEvaluation();

        bool isRunning = false;

        bool isCancelled = false;

        enum Mode {
            IterationTesting,
            LiveDebug
        }

        Mode currentMode = Mode.IterationTesting;

        void Start()
        {
            toggleButton.onClick.AddListener(OnToggleButtonClicked);
            switchButton.onClick.AddListener(OnSwitchButtonClicked);
        }

        Mat GetCameraIntrinsics()
        {
            float fx = (activeCamera.focalLength * activeCamera.pixelWidth) / activeCamera.sensorSize.x;
            float fy = (activeCamera.focalLength * activeCamera.pixelHeight) / activeCamera.sensorSize.y;

            float cx = activeCamera.pixelWidth / 2;
            float cy = activeCamera.pixelHeight / 2;

            Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            camMatrix.put(0, 0, fx);
            camMatrix.put(0, 1, 0);
            camMatrix.put(0, 2, cx);
            camMatrix.put(1, 0, 0);
            camMatrix.put(1, 1, fy);
            camMatrix.put(1, 2, cy);
            camMatrix.put(2, 0, 0);
            camMatrix.put(2, 1, 0);
            camMatrix.put(2, 2, 1.0f);

            return camMatrix;
        }

        void OnToggleButtonClicked()
        {
            if (currentMode == Mode.IterationTesting) 
            {
                int iterations = iterationsInput.text == "" ? 1 : int.Parse(iterationsInput.text);
                if (!isRunning)
                {
                    isCancelled = false;
                    isRunning = true;
                    StartEvaluation(iterations);
                    toggleButtonText.text = "Stop";
                }
                else
                {
                    isCancelled = true;
                    isRunning = false;
                    toggleButtonText.text = "Start in iterations";
                }
            }
            else if (currentMode == Mode.LiveDebug) {
                randomizePosition();
                randomizeRotation();

                ComputePredictionAndTrueData();
            }
        }

        void OnSwitchButtonClicked()
        {
            if (currentMode == Mode.IterationTesting) 
            {
                toggleButtonText.text = "Randomize Transformation";
                switchButtonText.text = "Switch to Iterations testing";
                currentMode = Mode.LiveDebug;
            }   
            else if (currentMode == Mode.LiveDebug) 
            {
                toggleButtonText.text = "Start in iterations";
                switchButtonText.text = "Switch to live debugging";
                currentMode = Mode.IterationTesting;
            } 
        }

        /// <summary>
        /// Capture the image from the active camera as a Texture2D
        /// </summary>
        /// <returns>Note that this texture should be destroyed at the end of it's life cycle.</returns>
        Texture2D CaptureFromActiveCamera() {
            int mHeight = activeCamera.pixelHeight;
            int mWidth = activeCamera.pixelWidth;

            var rect = new UnityEngine.Rect(0, 0, mWidth, mHeight);
            RenderTexture renderTexture = new RenderTexture(mWidth, mHeight, 24);
            Texture2D screenShot = new Texture2D(mWidth, mHeight, TextureFormat.RGBA32, false);
            activeCamera.targetTexture = renderTexture;

            activeCamera.Render();

            RenderTexture.active = renderTexture;
            screenShot.ReadPixels(rect, 0, 0);

            return screenShot;
        }

        /// <summary>
        /// Detect and shows the prediction from the window currently shown on the camera (instead of procedural testing)
        /// </summary>
        void FixedUpdate()
        {
            if (isRunning) return;
#if UNITY_EDITOR
            //This code is only for when we are able to move the object freely (currently not implemented in builds)
            //To help with debugging with detectors
            // ComputePredictionAndTrueData();
#endif
        }
        
        /// <summary>
        /// Compute and populate the predR, trueR, predP, trueP fields for the OnDrawGizmos method
        /// </summary>
        void ComputePredictionAndTrueData() {
            Mat intrinsics = GetCameraIntrinsics();

            var markerLength = arucoPlane.transform.localScale.x*10;
            
            Texture2D screenShot = CaptureFromActiveCamera();

            Matrix4x4 predRes = spacialEvaluation.ObtainPoseFromImage(screenShot, markerLength, intrinsics);
            Matrix4x4 truth = GetGroundTruth();

            // for OnDrawGizmos
            predR = ExtractRotationFromMatrix(predRes);
            trueR = ExtractRotationFromMatrix(truth);

            predP = ExtractPositionFromMatrix(predRes);
            trueP = ExtractPositionFromMatrix(truth);

            ShowDebugOffset(predRes);

            Destroy(screenShot);
        }

        void ShowDebugOffset(Matrix4x4 predRes){
            
            string info = $"truth: {trueR} \n prediction: {predR}";
            info += $"\noffset angle: {ComputeQuaternionError(trueR, predR)}";
            if (predRes == Matrix4x4.identity) info += "\nNot detected";
            infoText.text = info;
        }

        Quaternion predR = Quaternion.identity;
        Quaternion trueR = Quaternion.identity;
        Vector3 predP = new Vector3();
        Vector3 trueP = new Vector3();

        void OnDrawGizmos()
        {
            Quaternion predRot = predR;
            Quaternion truthRot = trueR;

            // Get positions (you might use the translation part of the TRS)
            Vector3 posPred = predP;
            Vector3 posTruth = trueP;

            // Draw truth axes at its position (red = right, green = up, blue = forward)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(posTruth, posTruth + truthRot * Vector3.right);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(posTruth, posTruth + truthRot * Vector3.up);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(posTruth, posTruth + truthRot * Vector3.forward);

            // Draw predicted axes offset a bit for clarity
            Vector3 offsetPos = posPred + Vector3.right * 0.5f;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(offsetPos, offsetPos + predRot * Vector3.right);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(offsetPos, offsetPos + predRot * Vector3.up);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(offsetPos, offsetPos + predRot * Vector3.forward);
        }

        //TODO: Potentially move this to th SpacialEvaluation class
        void StartEvaluation(int iterations)
        {
            //Potentially randomize camera intrinsics
            Mat intrinsics = GetCameraIntrinsics();

            float totalTime = 0;
            float matrixError = 0;
            float translationError = 0;
            float rotationError = 0;

            int validCount = 0;
            // Assuming marker is scaled proportionally in all directions.
            var markerLength = arucoPlane.transform.localScale.x*10;
            for (int i = 0; i < iterations; i++)
            {
                randomizePosition();
                randomizeRotation();
                Texture2D screenShot = CaptureFromActiveCamera();

                if (isCancelled) return;

                float t = Time.realtimeSinceStartup;

                Matrix4x4 predRes = spacialEvaluation.ObtainPoseFromImage(screenShot, markerLength, intrinsics);
                
                totalTime += Time.realtimeSinceStartup - t;
                Matrix4x4 truth = GetGroundTruth();

                if (predRes != Matrix4x4.identity)
                {
                    matrixError += GetFrobeniusNorm(truth, predRes);
                    translationError += GetTranslationError(truth, predRes);
                    rotationError += GetRotationalError(truth, predRes);
                    validCount++;
                }

                Destroy(screenShot);
            }

            toggleButtonText.text = "Start";
            infoText.text = $"Matrix error: {matrixError}/{validCount}\n"
            + $"Translation error: {translationError}/{validCount}\n"
            + $"Rotation error: {rotationError}/{validCount}\n"
            + $"Time in second: {totalTime}";

            EvalParam evalParam = GetEvalParam(iterations);
            EvalCameraIntrinsics evalCam = GetEvalCamIntrinsicsInfo();
            EvalSetupInfo evalSetup = GetEvalSetupInfo();
            EvalInfo evalInfo = new EvalInfo 
            {
                evalParam=evalParam,
                cameraIntrinsics=evalCam,
                setupInfo=evalSetup,
                totalTime=totalTime,
                matrixError=matrixError,
                translationError=translationError,
                rotationError=rotationError,
                validScanCount=validCount
            };

            spacialEvaluation.WriteResultToFile(evalInfo);
            isRunning = false;
        }

        /// <summary>
        /// Get the info for writing the evaluation to a file
        /// </summary>
        /// <returns></returns>
        EvalParam GetEvalParam(int totalCount) 
        {

            return new EvalParam 
            {
                deviceModel = SystemInfo.deviceModel,
                deviceName = SystemInfo.deviceName,
                deviceType = SystemInfo.deviceType,
                totalCount = totalCount,
            };
        }

        /// <summary>
        /// Get the info for writing the evaluation to a file
        /// </summary>
        /// <returns></returns>
        EvalCameraIntrinsics GetEvalCamIntrinsicsInfo() {
            Vector2 focalLength;
            focalLength.x = (activeCamera.focalLength * activeCamera.pixelWidth) / activeCamera.sensorSize.x;
            focalLength.y = (activeCamera.focalLength * activeCamera.pixelHeight) / activeCamera.sensorSize.y;

            Vector2 principalPoint;
            principalPoint.x = activeCamera.pixelWidth / 2;
            principalPoint.y = activeCamera.pixelHeight / 2;

            return new EvalCameraIntrinsics
            {
                focalLength = focalLength,
                principalPoint = principalPoint
            };
        }

        /// <summary>
        /// Get the info for writing the evaluation to a file
        /// </summary>
        /// <returns></returns>
        EvalSetupInfo GetEvalSetupInfo() {
            return new EvalSetupInfo
            {
                positionSetup = randomPositionSetup,
                rotationSetup = randomRotationSetup
            };
        }
 
        Matrix4x4 GetGroundTruth()
        {
            Transform t = arucoPlane.transform;

            Matrix4x4 trsMat = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
            return activeCamera.transform.localToWorldMatrix * trsMat;
        }
        
        readonly Vector3Setup randomPositionSetup = new Vector3Setup 
        {
            xVariation = new Vector2(0.2f, 0.8f),
            yVariation = new Vector2(0.2f, 0.8f),
            zVariation = new Vector2(3, 6),
        };

        readonly Vector3Setup randomRotationSetup = new Vector3Setup
        {
            xVariation = new Vector2(-40, -150),
            yVariation = new Vector2(-50, 50),
            zVariation = new Vector2(-50, 50),
        };

        void randomizePosition()
        {
            Vector3 viewportPos = new Vector3();

            viewportPos.x = Random.Range(randomPositionSetup.xVariation.x, randomPositionSetup.xVariation.y);
            viewportPos.y = Random.Range(randomPositionSetup.yVariation.x, randomPositionSetup.yVariation.y);
            viewportPos.z = Random.Range(randomPositionSetup.zVariation.x, randomPositionSetup.zVariation.y);

            arucoPlane.transform.position = activeCamera.ViewportToWorldPoint(viewportPos);
        }

        void randomizeRotation()
        {
            // Randomize x and z of parent
            // Values are tested relative to camera's viewport to make sure it is visible
            Vector3 parentRotation = new Vector3();
            parentRotation.x = Random.Range(randomRotationSetup.xVariation.x, randomRotationSetup.xVariation.y);
            parentRotation.y = Random.Range(randomRotationSetup.yVariation.x, randomRotationSetup.yVariation.y);
            parentRotation.z = Random.Range(randomRotationSetup.zVariation.x, randomRotationSetup.zVariation.y);
            arucoPlane.transform.rotation = Quaternion.Euler(parentRotation);
        }
    }

}
