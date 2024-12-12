#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

using TMPro;

namespace CakeLab.ARFlow.ArUcoTracking
{

    public delegate void OnSpaceSynced();

    /// <summary>
    /// ARFoundationCamera ArUco Example
    /// An example of ArUco marker detection from an ARFoundation camera image.
    /// </summary>
    [RequireComponent(typeof(ARFoundationCamera2MatHelper))]
    public class ARFoundationCameraArUco : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The raw camera image.
        /// </summary>
        public RawImage rawCameraImage;

        [Space(10)]

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_250;

        /// <summary>
        /// The dictionary id dropdown.
        /// </summary>
        public Dropdown dictionaryIdDropdown;

        /// <summary>
        /// Determines if applied the pose estimation.
        /// </summary>
        public bool applyEstimationPose = true;

        // /// <summary>
        // /// The apply estimation pose toggle.
        // /// </summary>
        // public Toggle applyEstimationPoseToggle;

        [Space(10)]

        /// <summary>
        /// The length of the markers' side. Normally, unit is meters.
        /// </summary>
        public float markerLength = 0.188f;

        public TMP_InputField inputMarkerLength;

        /// <summary>
        /// The AR game object.
        /// </summary>
        public ARGameObject arGameObject;

        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera arCamera;

        [Space(10)]

        /// <summary>
        /// Determines if enable leap filter.
        /// </summary>
        public bool enableLerpFilter = true;

        [Space(10)]
        public Button flipHorizontalButton;
        void onFlipHorizontalButtonClick()
        {
            OnStopScanning();
            webCamTextureToMatHelper.flipHorizontal = !webCamTextureToMatHelper.flipHorizontal;
            OnStartScanning();
        }
        public Button flipVerticalButton;
        void onFlipVerticalButtonClick()
        {
            OnStopScanning();
            webCamTextureToMatHelper.flipVertical = !webCamTextureToMatHelper.flipVertical;
            OnStartScanning();
        }
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        ARFoundationCamera2MatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The distortion coeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

        // /// <summary>
        // /// The FPS monitor.
        // /// </summary>
        // FpsMonitor fpsMonitor;

        // for CanonicalMarker.
        Mat ids;
        List<Mat> corners;
        List<Mat> rejectedCorners;
        Dictionary dictionary;
        ArucoDetector arucoDetector;

        Mat rvecs;
        Mat tvecs;


        Matrix4x4 fitARFoundationBackgroundMatrix;
        Matrix4x4 fitHelpersFlipMatrix;

        private OnSpaceSynced m_OnSpaceSynced;
        public OnSpaceSynced OnSpaceSynced
        {
            get { return m_OnSpaceSynced; }
            set { m_OnSpaceSynced = value; }
        }

        // Use this for initialization
        void Start()
        {
            flipHorizontalButton.onClick.AddListener(onFlipHorizontalButtonClick);
            flipVerticalButton.onClick.AddListener(onFlipVerticalButtonClick);

            inputMarkerLength.text = markerLength.ToString();


            webCamTextureToMatHelper = gameObject.GetComponent<ARFoundationCamera2MatHelper>();
            webCamTextureToMatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
            webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
#endif // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
            webCamTextureToMatHelper.Initialize();

            dictionaryIdDropdown.value = (int)dictionaryId;
            // applyEstimationPoseToggle.isOn = applyEstimationPose;
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            //Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat rgbaMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            rawCameraImage.texture = texture;
            float heightScale = (float)rgbaMat.height() / rgbaMat.width();
            rawCameraImage.rectTransform.sizeDelta = new Vector2(640, 640 * heightScale);

            //Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            // set camera parameters.
            double fx;
            double fy;
            double cx;
            double cy;

            // Through testing, most android device cameras are mirrored when obtaining image through ARFoundation
#if (UNITY_ANDROID)
            webCamTextureToMatHelper.flipHorizontal = webCamTextureToMatHelper.IsFrontFacing();
            webCamTextureToMatHelper.flipVertical = true;
#else
            webCamTextureToMatHelper.flipHorizontal = !webCamTextureToMatHelper.IsFrontFacing();
#endif

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
            UnityEngine.XR.ARSubsystems.XRCameraIntrinsics cameraIntrinsics = webCamTextureToMatHelper.GetCameraIntrinsics();

            // Apply the rotate and flip properties of camera helper to the camera intrinsics.
            Vector2 fl = cameraIntrinsics.focalLength;
            Vector2 pp = cameraIntrinsics.principalPoint;
            Vector2Int r = cameraIntrinsics.resolution;

            Matrix4x4 tM = Matrix4x4.Translate(new Vector3(-r.x / 2, -r.y / 2, 0));
            pp = tM.MultiplyPoint3x4(pp);

            Matrix4x4 rotationAndFlipM = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, webCamTextureToMatHelper.rotate90Degree ? 90 : 0),
                new Vector3(webCamTextureToMatHelper.flipHorizontal ? -1 : 1, webCamTextureToMatHelper.flipVertical ? -1 : 1, 1));
            pp = rotationAndFlipM.MultiplyPoint3x4(pp);

            if (webCamTextureToMatHelper.rotate90Degree)
            {
                fl = new Vector2(fl.y, fl.x);
                r = new Vector2Int(r.y, r.x);
            }

            Matrix4x4 _tM = Matrix4x4.Translate(new Vector3(r.x / 2, r.y / 2, 0));
            pp = _tM.MultiplyPoint3x4(pp);

            cameraIntrinsics = new UnityEngine.XR.ARSubsystems.XRCameraIntrinsics(fl, pp, r);

            fx = cameraIntrinsics.focalLength.x;
            fy = cameraIntrinsics.focalLength.y;
            cx = cameraIntrinsics.principalPoint.x;
            cy = cameraIntrinsics.principalPoint.y;

            camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            camMatrix.put(0, 0, fx);
            camMatrix.put(0, 1, 0);
            camMatrix.put(0, 2, cx);
            camMatrix.put(1, 0, 0);
            camMatrix.put(1, 1, fy);
            camMatrix.put(1, 2, cy);
            camMatrix.put(2, 0, 0);
            camMatrix.put(2, 1, 0);
            camMatrix.put(2, 2, 1.0f);

            distCoeffs = new MatOfDouble(0, 0, 0, 0);

            //Debug.Log("Created CameraParameters from the camera intrinsics to be populated if the camera supports intrinsics.");

            var focalLength = cameraIntrinsics.focalLength;
            var principalPoint = cameraIntrinsics.principalPoint;
            var resolution = cameraIntrinsics.resolution;

            // if (fpsMonitor != null)
            // {
            //     fpsMonitor.Add("cameraIntrinsics", "\n" + "FL: " + focalLength.x + "x" + focalLength.y + "\n" + "PP: " + principalPoint.x + "x" + principalPoint.y + "\n" + "R: " + resolution.x + "x" + resolution.y);
            // }

#else // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
            float width = rgbaMat.width();
            float height = rgbaMat.height();

            int max_d = (int)Mathf.Max(width, height);
            fx = max_d;
            fy = max_d;
            cx = width / 2.0f;
            cy = height / 2.0f;

            camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            camMatrix.put(0, 0, fx);
            camMatrix.put(0, 1, 0);
            camMatrix.put(0, 2, cx);
            camMatrix.put(1, 0, 0);
            camMatrix.put(1, 1, fy);
            camMatrix.put(1, 2, cy);
            camMatrix.put(2, 0, 0);
            camMatrix.put(2, 1, 0);
            camMatrix.put(2, 2, 1.0f);

            distCoeffs = new MatOfDouble(0, 0, 0, 0);

            //Debug.Log("Created a dummy CameraParameters.");

#endif // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

            //Debug.Log("camMatrix " + camMatrix.dump());
            //Debug.Log("distCoeffs " + distCoeffs.dump());


            rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);


            ids = new Mat();
            corners = new List<Mat>();
            rejectedCorners = new List<Mat>();
            rvecs = new Mat(1, 10, CvType.CV_64FC3);
            tvecs = new Mat(1, 10, CvType.CV_64FC3);
            dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);

            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_useAruco3Detection(true);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);


            // Create the transform matrix to fit the ARM to the background display by ARFoundationBackground component.
            fitARFoundationBackgroundMatrix = Matrix4x4.Scale(new Vector3(webCamTextureToMatHelper.GetDisplayFlipHorizontal() ? -1 : 1, webCamTextureToMatHelper.GetDisplayFlipVertical() ? -1 : 1, 1)) * Matrix4x4.identity;

            // Create the transform matrix to fit the ARM to the flip properties of the camera helper.
            fitHelpersFlipMatrix = Matrix4x4.Scale(new Vector3(webCamTextureToMatHelper.flipHorizontal ? -1 : 1, webCamTextureToMatHelper.flipVertical ? -1 : 1, 1)) * Matrix4x4.identity;
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            //Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (rgbMat != null)
                rgbMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (ids != null)
                ids.Dispose();
            foreach (var item in corners)
            {
                item.Dispose();
            }
            corners.Clear();
            foreach (var item in rejectedCorners)
            {
                item.Dispose();
            }
            rejectedCorners.Clear();
            if (rvecs != null)
                rvecs.Dispose();
            if (tvecs != null)
                tvecs.Dispose();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            //Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode + ":" + message);

        }

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

        void OnFrameMatAcquired(Mat mat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, XRCameraIntrinsics cameraIntrinsics, long timestamp)
        {
            Mat rgbaMat = mat;

            Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

            // detect markers.
            Calib3d.undistort(rgbMat, rgbMat, camMatrix, distCoeffs);
            arucoDetector.detectMarkers(rgbMat, corners, ids, rejectedCorners);

            Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
            Utils.matToTexture2D(rgbaMat, texture);

            // if at least one marker detected
            if (ids.total() > 0)
            {
                // draw markers.
                Objdetect.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));
                // estimate pose.
                if (applyEstimationPose)
                {
                    EstimatePoseCanonicalMarker(rgbMat);
                }
                m_OnSpaceSynced?.Invoke();
            }

            //Imgproc.putText (rgbMat, "W:" + rgbMat.width () + " H:" + rgbMat.height () + " SO:" + Screen.orientation, new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);


        }

#else // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

                // detect markers.
                Calib3d.undistort(rgbMat, rgbMat, camMatrix, distCoeffs);
                arucoDetector.detectMarkers(rgbMat, corners, ids, rejectedCorners);

                Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);
                Utils.matToTexture2D(rgbaMat, texture);

                // if at least one marker detected
                if (ids.total() > 0)
                {
                    // draw markers.
                    Objdetect.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));

                    // estimate pose.
                    if (applyEstimationPose)
                    {
                        EstimatePoseCanonicalMarker(rgbMat);
                    }
                    m_OnSpaceSynced?.Invoke();

                }

                //Imgproc.putText (rgbMat, "W:" + rgbMat.width () + " H:" + rgbMat.height () + " SO:" + Screen.orientation, new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            }
        }

#endif // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

        private void EstimatePoseCanonicalMarker(Mat rgbMat)
        {
            using (MatOfPoint3f objPoints = new MatOfPoint3f(
                new Point3(-markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, -markerLength / 2f, 0),
                new Point3(-markerLength / 2f, -markerLength / 2f, 0)
            ))
            {
                for (int i = 0; i < ids.total(); i++)
                {
                    using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat corner_4x1 = corners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                    using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                    {
                        // Calculate pose for each marker
                        Calib3d.solvePnP(objPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

                        // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                        // This example can display the ARObject on only first detected marker.
                        if (i == 0)
                        {
                            UpdateARObjectTransform(rvec, tvec);
                        }
                    }
                }
            }
        }

        private void UpdateARObjectTransform(Mat rvec, Mat tvec)
        {

            //Debug.Log(rvec.dump());
            //Debug.Log(tvec.dump());

            // Convert to unity pose data.
            double[] rvecArr = new double[3];
            rvec.get(0, 0, rvecArr);
            double[] tvecArr = new double[3];
            tvec.get(0, 0, tvecArr);
            PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

            // Convert to transform matrix.
            ARM = ARUtils.ConvertPoseDataToMatrix(ref poseData, true);
            //Debug.Log("ARM " + ARM.ToString());

            // Apply the effect (flipping factors) of the projection matrix applied to the ARCamera by the ARFoundationBackground component to the ARM.
            ARM = fitARFoundationBackgroundMatrix * ARM;

            // When detecting the AR marker from a horizontal inverted image (front facing camera),
            // will need to apply an inverted X matrix to the transform matrix to match the ARFoundationBackground component display.
            ARM = fitHelpersFlipMatrix * ARM;

            ARM = arCamera.transform.localToWorldMatrix * ARM;

            if (enableLerpFilter)
            {
                //Debug.Log("ARM " + ARM.ToString());
                arGameObject.SetMatrix4x4(ARM);
            }
            else
            {
                ARUtils.SetTransformFromMatrix(arGameObject.transform, ref ARM);
            }
        }

        private void ResetObjectTransform()
        {
            // reset AR object transform.
            Matrix4x4 i = Matrix4x4.identity;
            ARUtils.SetTransformFromMatrix(arGameObject.transform, ref i);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
            webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
#endif // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API
            webCamTextureToMatHelper.Dispose();
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnStartScanning()
        {
            if (float.Parse(inputMarkerLength.text) > 0)
                markerLength = float.Parse(inputMarkerLength.text);
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopScanning()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if ((int)dictionaryId != result)
            {
                dictionaryId = (ArUcoDictionary)result;
                dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);

                ResetObjectTransform();

                if (webCamTextureToMatHelper != null && webCamTextureToMatHelper.IsInitialized())
                    webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the apply estimation P\pose toggle value changed event.
        /// </summary>
        public void OnApplyEstimationPoseToggleValueChanged()
        {
            // applyEstimationPose = applyEstimationPoseToggle.isOn;
        }

        /// <summary>
        /// Raises the change LightEstimation button click event.
        /// </summary>
        public void OnChangeLightEstimationButtonClick()
        {
            if (webCamTextureToMatHelper != null && webCamTextureToMatHelper.IsInitialized())
            {
                webCamTextureToMatHelper.requestedLightEstimation = (webCamTextureToMatHelper.requestedLightEstimation == LightEstimation.None)
                    ? LightEstimation.AmbientColor | LightEstimation.AmbientIntensity
                    : LightEstimation.None;
            }
        }

        public enum ArUcoDictionary
        {
            DICT_4X4_50 = Objdetect.DICT_4X4_50,
            DICT_4X4_100 = Objdetect.DICT_4X4_100,
            DICT_4X4_250 = Objdetect.DICT_4X4_250,
            DICT_4X4_1000 = Objdetect.DICT_4X4_1000,
            DICT_5X5_50 = Objdetect.DICT_5X5_50,
            DICT_5X5_100 = Objdetect.DICT_5X5_100,
            DICT_5X5_250 = Objdetect.DICT_5X5_250,
            DICT_5X5_1000 = Objdetect.DICT_5X5_1000,
            DICT_6X6_50 = Objdetect.DICT_6X6_50,
            DICT_6X6_100 = Objdetect.DICT_6X6_100,
            DICT_6X6_250 = Objdetect.DICT_6X6_250,
            DICT_6X6_1000 = Objdetect.DICT_6X6_1000,
            DICT_7X7_50 = Objdetect.DICT_7X7_50,
            DICT_7X7_100 = Objdetect.DICT_7X7_100,
            DICT_7X7_250 = Objdetect.DICT_7X7_250,
            DICT_7X7_1000 = Objdetect.DICT_7X7_1000,
            DICT_ARUCO_ORIGINAL = Objdetect.DICT_ARUCO_ORIGINAL,
        }
    }
}

#endif