using UnityEngine;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;

// using CakeLab.ARFlow.;

namespace CakeLab.ARFlow.Evaluation
{
    using System.Collections.Generic;
    using OpenCVForUnityExample;
    using TMPro;
    using UnityEngine.UI;



    /// <summary>
    /// Class to evaluate the spacial accuracy of the ARFlow system.
    /// 

    public class ArUcoSpacialEvaluation : SpacialEvaluation
    {
        List<Mat> _corners;
        List<Mat> _rejectedCorners;


        Mat _rgbMat;


        Mat _camIntrinsics;
        Mat ids;
        MatOfDouble _distCoeffs;
        Dictionary _dictionary;
        ArucoDetector _arucoDetector;

        //TODO: configure. For now the default (and used ArUco marker) is 4x4 50
        public int dictionaryId = Objdetect.DICT_4X4_50;

        public ArUcoSpacialEvaluation()
        {

            ids = new Mat();
            _corners = new List<Mat>();
            _rejectedCorners = new List<Mat>();
            _rgbMat = new Mat(); // Initialize _rgbMat
            _distCoeffs = new MatOfDouble(0, 0, 0, 0);

            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_useAruco3Detection(true);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            _dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);
            _arucoDetector = new ArucoDetector(_dictionary, detectorParams, refineParameters);

        }


        /// <summary>
        /// Obtain the pose of the camera from the captured image.
        /// Pose is displayed as the localToWorldMatrix.
        /// </summary>
        /// <param name="capturedImage"></param>
        /// <returns></returns>
        // public abstract Matrix4x4 ObtainPoseFromImage(Texture2D capturedImage, float markerLength, Mat camIntrinsics);
        public Matrix4x4 ObtainPoseFromImage(Texture2D capturedImage, float markerLength, Mat camIntrinsics)
        {
            if (camIntrinsics != null)
            {
                _camIntrinsics = camIntrinsics;
            }

            // Initialize _rgbMat with the dimensions of the captured image
            if (_rgbMat.empty())
            {
                _rgbMat = new Mat(capturedImage.height, capturedImage.width, CvType.CV_8UC3);
            }

            Utils.texture2DToMat(capturedImage, _rgbMat);
            _arucoDetector.detectMarkers(_rgbMat, _corners, ids, _rejectedCorners);

            return EstimatePoseCanonicalMarker(_rgbMat, markerLength);
        }

        private Matrix4x4 EstimatePoseCanonicalMarker(Mat rgbMat, float markerLength)
        {
            using (MatOfPoint3f objPoints = new MatOfPoint3f(
                new Point3(-markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, -markerLength / 2f, 0),
                new Point3(-markerLength / 2f, -markerLength / 2f, 0)
            ))
            {
                if (ids.total() > 0)
                {
                    using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat corner_4x1 = _corners[0].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                    using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                    {
                        // Calculate pose marker
                        Calib3d.solvePnP(objPoints, imagePoints, _camIntrinsics, _distCoeffs, rvec, tvec);

                        // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        Calib3d.drawFrameAxes(rgbMat, _camIntrinsics, _distCoeffs, rvec, tvec, markerLength * 0.5f);

                        return GetpositionAndQuaternion(rvec, tvec);
                    }
                }
            }


            return Matrix4x4.identity;
        }

        private Matrix4x4 GetpositionAndQuaternion(Mat rvec, Mat tvec)
        {
            // Convert to unity pose data.
            double[] rvecArr = new double[3];
            rvec.get(0, 0, rvecArr);
            double[] tvecArr = new double[3];
            tvec.get(0, 0, tvecArr);
            Debug.Log(rvecArr);
            Debug.Log(tvecArr);
            PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

            // Convert to transform matrix.
            return ARUtils.ConvertPoseDataToMatrix(ref poseData, true);
        }

        public void dispose()
        {
            _camIntrinsics.Dispose();
            _distCoeffs.Dispose();
            ids.Dispose();
            foreach (var corner in _corners)
            {
                corner.Dispose();
            }
            foreach (var corner in _rejectedCorners)
            {
                corner.Dispose();
            }
        }
    }

    public class SpacialEvaluationManager : MonoBehaviour
    {

        //The textured plane, child to the arucoObj. 
        //In the current implementation, this plane is rotated by 90 degrees along the y axis. 
        // This is to make sure that the initial state (unrotated)
        public GameObject texturedPlane;

        // The ARPlane object that will be moved, child of the arucoParent
        public GameObject arucoObj;
        // The parent object of the ARPlane object. These two objects are for rotation purposes.
        public GameObject arucoParent;

        public Camera activeCamera;

        public TMP_Text infoText;
        public Button toggleButton;
        public TMP_Text buttonText;

        public TMP_InputField iterationsInput;


        ArUcoSpacialEvaluation spacialEvaluation = new ArUcoSpacialEvaluation();

        bool isRunning = false;

        bool isCancelled = false;



        void Start()
        {
            toggleButton.onClick.AddListener(OnButtonClicked);
        }

        Mat GetCameraIntrinsics()
        {
            float f = activeCamera.focalLength;
            float fx = f;
            float fy = f;

            float cx = activeCamera.sensorSize.x / 2;
            float cy = activeCamera.sensorSize.y / 2;

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

        void OnButtonClicked()
        {
            int iterations = iterationsInput.text == "" ? 1 : int.Parse(iterationsInput.text);
            if (!isRunning)
            {
                isCancelled = false;
                isRunning = true;
                StartEvaluation(iterations);
                buttonText.text = "Stop";
            }
            else
            {
                isCancelled = true;
                isRunning = false;
                buttonText.text = "Start";
            }
        }

        void StartEvaluation(int iterations)
        {
            //Potentially randomize camera intrinsics
            Mat intrinsics = GetCameraIntrinsics();

            float errorScore = 0;
            int validCount = 0;

            int mHeight = activeCamera.pixelHeight;
            int mWidth = activeCamera.pixelWidth;

            var rect = new UnityEngine.Rect(0, 0, mWidth, mHeight);
            RenderTexture renderTexture = new RenderTexture(mWidth, mHeight, 24);
            Texture2D screenShot = new Texture2D(mWidth, mHeight, TextureFormat.RGBA32, false);

            activeCamera.targetTexture = renderTexture;

            // Assuming marker is scaled proportionally in all directions, and units are in meters.
            var markerLength = texturedPlane.transform.localScale.x;

            for (int i = 0; i < iterations; i++)
            {
                if (isCancelled) return;
                randomizePosition();
                randomizeRotation();

                activeCamera.Render();

                RenderTexture.active = renderTexture;
                screenShot.ReadPixels(rect, 0, 0);

                Matrix4x4 evalRes = spacialEvaluation.ObtainPoseFromImage(screenShot, markerLength, intrinsics);
                Matrix4x4 truth = GetGroundTruth();

                if (evalRes != Matrix4x4.identity)
                {
                    errorScore += SpacialEvaluation.GetErrorScore(truth, evalRes);
                    validCount++;
                }
                Debug.Log(errorScore);
                Debug.Log(evalRes);
                Debug.Log(truth);
                Debug.Log("---------------------");


                infoText.text = errorScore.ToString();
            }

            buttonText.text = "Start";
            infoText.text = $"{errorScore.ToString()}/{validCount}";
            isRunning = false;

        }

        Matrix4x4 GetGroundTruth()
        {
            Transform t = arucoObj.transform;
            // Vector3 relativePosition = activeCamera.transform.InverseTransformDirection(t.position - activeCamera.transform.position);


            // return Matrix4x4.TRS(relativePosition, t.rotation, Vector3.one);
            return Matrix4x4.TRS(t.position, t.rotation, Vector3.one);


        }

        void randomizePosition()
        {
            // Randomize the position of the object
            // ranges are set to be of adequate size to fit the camera viewport.
            Vector3 viewportPos = new Vector3();

            viewportPos.x = Random.Range(0.1f, 0.9f);
            viewportPos.y = Random.Range(0.1f, 0.9f);
            viewportPos.z = Random.Range(3, 6);

            Vector3 pos = activeCamera.ViewportToWorldPoint(viewportPos);
            Debug.Log(pos);
            arucoParent.transform.position = activeCamera.ViewportToWorldPoint(viewportPos);
        }

        void randomizeRotation()
        {
            // Randomize x and z of parent
            // Values are tested relative to camera's viewport, so that it is visible
            Vector3 parentRotation = new Vector3();
            parentRotation.x = Random.Range(-100, -50);
            parentRotation.z = Random.Range(-30, 30);
            arucoParent.transform.rotation = Quaternion.Euler(parentRotation);

            //randomize y of plane
            Vector3 planeRotation = new Vector3();
            planeRotation.y = Random.Range(0, 360);
            planeRotation.x = 0;
            planeRotation.z = 0;
            arucoObj.transform.localRotation = Quaternion.Euler(planeRotation);
        }
    }


}
