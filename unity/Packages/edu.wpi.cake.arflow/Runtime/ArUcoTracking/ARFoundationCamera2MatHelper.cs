//Vendored from https://github.com/EnoxSoftware/ARFoundationWithOpenCVForUnityExample/tree/master/Assets/ARFoundationWithOpenCVForUnityExample/Scripts/Utils
// With modifications to work with OpenCVForUnity 3.4.0
#pragma warning disable 0067
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Linq;
using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace CakeLab.ARFlow.ArUcoTracking
{
    /// <summary>
    /// This is called every time there is a new frame image mat available.
    /// The Mat object's type is 'CV_8UC4' or 'CV_8UC3' or 'CV_8UC1' (ColorFormat is determined by the outputColorFormat setting).
    /// </summary>
    /// <param name="mat">The recently captured frame image mat.</param>
    /// <param name="projectionMatrix">The projection matrices.</param>
    /// <param name="cameraToWorldMatrix">The camera to world matrices.</param>
    /// <param name="cameraIntrinsics">The camera intrinsics.</param>
    /// <param name="timestamp">The camera timestamp in nanoseconds.</param>
    public delegate void FrameMatAcquiredCallback(Mat mat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix, XRCameraIntrinsics cameraIntrinsics, long timestamp);

    /// <summary>
    /// A helper component class for obtaining camera frames from ARFoundation and converting them to OpenCV <c>Mat</c> format in real-time.
    /// </summary>
    /// <remarks>
    /// The <c>ARFoundationCamera2MatHelper</c> class captures video frames from a device's camera using Unity's ARFoundation framework
    /// and converts each frame to an OpenCV <c>Mat</c> object every frame. In addition to the camera frame data, this class provides access 
    /// to ARFoundation's world matrix and projection matrix, enabling applications to perform computer vision tasks in an AR context.
    /// 
    /// This component handles necessary transformations and adjustments to align the <c>Mat</c> output with ARFoundation's camera settings,
    /// ensuring consistency with the device's display orientation and AR session data. It is particularly useful for integrating 
    /// OpenCV-based image processing algorithms with Unity's AR capabilities.
    /// 
    /// <strong>Note:</strong> By setting outputColorFormat to RGBA, processing that does not include extra color conversion is performed.
    /// <strong>Note:</strong> Depends on ARFoundation version 5.1.5 or later.
    /// <strong>Note:</strong> Depends on OpenCVForUnity version 2.6.4 or later.
    /// </remarks>
    /// <example>
    /// Attach this component to a GameObject and call <c>GetMat()</c> to retrieve the latest camera frame in <c>Mat</c> format. 
    /// Additionally, use <c>GetCameraToWorldMatrix()</c> and <c>GetProjectionMatrix()</c> to access AR camera matrices.
    /// The helper class manages AR session start/stop operations and frame updates internally.
    /// </example>
    public class ARFoundationCamera2MatHelper : WebCamTexture2MatHelper
    {

        /// <summary>
        /// This will be called whenever a new camera frame image available is converted to Mat.
        /// The Mat object's type is 'CV_8UC4' or 'CV_8UC3' or 'CV_8UC1' (ColorFormat is determined by the outputColorFormat setting).
        /// You must properly initialize the ARFoundationCamera2MatHelperf, 
        /// including calling Play() before this event will begin firing.
        /// </summary>
        public virtual event FrameMatAcquiredCallback frameMatAcquired;

        /// <summary>
        /// The XROrigin to get the linked AR Camera and ARCameraManager.
        /// </summary>
        [SerializeField, FormerlySerializedAs("xROrigin"), TooltipAttribute("The XROrigin to get the linked AR Camera and ARCameraManager.")]
        protected XROrigin _xROrigin;

        public virtual XROrigin xROrigin
        {
            get { return _xROrigin; }
            set { _xROrigin = value; }
        }

        protected XRCameraIntrinsics cameraIntrinsics;

        /// <summary>
        /// Returns the camera intrinsics.
        /// </summary>
        /// <returns>The camera intrinsics.</returns>
        public virtual XRCameraIntrinsics GetCameraIntrinsics()
        {
            return cameraIntrinsics;
        }

        protected ARCameraFrameEventArgs frameEventArgs;

        /// <summary>
        /// Returns the frameEventArgs.
        /// </summary>
        /// <returns>The frameEventArgs.</returns>
        public virtual ARCameraFrameEventArgs GetFrameEventArgs()
        {
            return frameEventArgs;
        }

        protected long timestampNs;

        /// <summary>
        /// Returns the timestampNs.
        /// </summary>
        /// <returns>The timestampNs.</returns>
        public virtual long GetTimestampNs()
        {
            return timestampNs;
        }

        protected ARCameraManager cameraManager = default;

        /// <summary>
        /// Returns the ARCameraManager.
        /// </summary>
        /// <returns>The ARCameraManager.</returns>
        public virtual ARCameraManager GetARCameraManager()
        {
            return cameraManager;
        }

        protected int displayRotationAngle = 0;

        /// <summary>
        /// Returns the display rotation angle.
        /// </summary>
        /// <returns>The display rotation angle.</returns>
        public virtual int GetDisplayRotationAngle()
        {
            return displayRotationAngle;
        }

        protected bool displayFlipVertical = false;

        /// <summary>
        /// Returns the display flip vertical.
        /// </summary>
        /// <returns>The display flip vertical.</returns>
        public virtual bool GetDisplayFlipVertical()
        {
            return displayFlipVertical;
        }

        protected bool displayFlipHorizontal = false;

        /// <summary>
        /// Returns the display flip horizontal.
        /// </summary>
        /// <returns>The display flip horizontal.</returns>
        public virtual bool GetDisplayFlipHorizontal()
        {
            return displayFlipHorizontal;
        }

        /// <summary>
        /// Transforms the mat to display direction.
        /// Please do not dispose of the returned mat as it will be reused.
        /// </summary>
        /// <param name="srcMat">srcMat.</param>
        /// <param name="dstMat">dstMat.</param>
        /// <returns>The transformed dstMat.</returns>
        public virtual Mat TransformMatToDisplayDirection(Mat srcMat, Mat dstMat)
        {
            if (srcMat == null)
                throw new ArgumentNullException("srcMat");
            if (srcMat != null)
                srcMat.ThrowIfDisposed();

            if (dstMat == null)
                throw new ArgumentNullException("dstMat");
            if (dstMat != null)
                dstMat.ThrowIfDisposed();

            if (srcMat == dstMat)
                throw new ArgumentNullException("srcMat == dstMat");

            if (rotatedFrameMat != null)
            {
                if (srcMat.rows() != dstMat.cols() || srcMat.cols() != dstMat.rows())
                {
                    Imgproc.resize(srcMat, dstMat, new Size(srcMat.rows(), srcMat.cols()));
                }

                if (displayRotationAngle == 90 || displayRotationAngle == 270)
                {
                    // (Orientation is Portrait, rotate90Degree is false)
                    bool _flipVertical = displayFlipVertical ? !flipHorizontal : flipHorizontal;
                    bool _flipHorizontal = displayFlipHorizontal ? !flipVertical : flipVertical;
                    FlipMat(srcMat, _flipVertical, _flipHorizontal, false, 0);
                }
                else
                {
                    // (Orientation is Landscape, rotate90Degrees is true)
                    bool _flipVertical = displayFlipVertical ? !flipVertical : flipVertical;
                    bool _flipHorizontal = displayFlipHorizontal ? !flipHorizontal : flipHorizontal;
                    FlipMat(srcMat, _flipVertical, _flipHorizontal, false, 0);
                }

                Core.rotate(srcMat, dstMat, Core.ROTATE_90_CLOCKWISE);
                return dstMat;
            }
            else
            {
                if (srcMat.rows() != dstMat.rows() || srcMat.cols() != dstMat.cols())
                {
                    Imgproc.resize(srcMat, dstMat, srcMat.size());
                }

                if (displayRotationAngle == 90 || displayRotationAngle == 270)
                {
                    // (Orientation is Portrait, rotate90Degree is true)
                    bool _flipVertical = displayFlipVertical ? flipHorizontal : !flipHorizontal;
                    bool _flipHorizontal = displayFlipHorizontal ? flipVertical : !flipVertical;
                    FlipMat(srcMat, _flipVertical, _flipHorizontal, true, 0);
                }
                else
                {
                    // (Orientation is Landscape, rotate90Degrees is false)
                    bool _flipVertical = displayFlipVertical ? !flipVertical : flipVertical;
                    bool _flipHorizontal = displayFlipHorizontal ? !flipHorizontal : flipHorizontal;
                    FlipMat(srcMat, _flipVertical, _flipHorizontal, true, 0);
                }

                srcMat.copyTo(dstMat);

                return dstMat;
            }
        }


        #region --ARFoundation CameraManager Properties--

        public virtual bool autoFocusEnabled => GetARCameraManager() != null ? GetARCameraManager().autoFocusEnabled : default;

        public virtual bool autoFocusRequested
        {
            get { return GetARCameraManager() != null ? GetARCameraManager().autoFocusRequested : default; }
            set
            {
                if (GetARCameraManager() == null)
                    return;

                if (GetARCameraManager().autoFocusRequested != value)
                {
                    GetARCameraManager().autoFocusRequested = value;

                    if (hasInitDone)
                        Initialize();
                    else if (isInitWaiting)
                        Initialize(autoPlayAfterInitialize);
                }
            }
        }

        public virtual CameraFacingDirection currentFacingDirection => GetARCameraManager() != null ? GetARCameraManager().currentFacingDirection : default;

        public virtual LightEstimation currentLightEstimation => GetARCameraManager() != null ? GetARCameraManager().currentLightEstimation : default;

        public virtual bool permissionGranted => GetARCameraManager() != null ? GetARCameraManager().permissionGranted : default;

        public virtual CameraFacingDirection requestedFacingDirection
        {
            get { return GetARCameraManager() != null ? GetARCameraManager().requestedFacingDirection : default; }
            set
            {
                if (GetARCameraManager() == null)
                    return;

                if (GetARCameraManager().requestedFacingDirection != value)
                {
                    GetARCameraManager().requestedFacingDirection = value;
                    _requestedIsFrontFacing = value == CameraFacingDirection.User;

                    if (hasInitDone)
                        Initialize();
                    else if (isInitWaiting)
                        Initialize(autoPlayAfterInitialize);
                }
            }
        }

        public virtual LightEstimation requestedLightEstimation
        {
            get { return GetARCameraManager() != null ? GetARCameraManager().requestedLightEstimation : default; }
            set
            {
                if (GetARCameraManager() == null)
                    return;

                if (GetARCameraManager().requestedLightEstimation != value)
                {
                    GetARCameraManager().requestedLightEstimation = value;

                    if (hasInitDone)
                        Initialize();
                    else if (isInitWaiting)
                        Initialize(autoPlayAfterInitialize);
                }
            }
        }

        #endregion


#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

        public override float requestedFPS
        {
            get { return _requestedFPS; }
            set
            {
                if (_requestedFPS != Mathf.Clamp(value, -1f, float.MaxValue))
                {
                    _requestedFPS = Mathf.Clamp(value, -1f, float.MaxValue);

                    if (hasInitDone)
                        Initialize();
                    else if (isInitWaiting)
                        Initialize(autoPlayAfterInitialize);
                }
            }
        }

        protected bool didUpdateThisFrame = false;
        protected bool didUpdatePreviewPixelBufferInCurrentFrame = false;

        protected bool isPlaying = false;

        protected int previewWidth = default;
        protected int previewHeight = default;
        protected int previewFramerate = -1;
        protected CameraFacingDirection previewFacingDirection = CameraFacingDirection.None;

        protected Mat pixelBufferMat;

        protected Matrix4x4 projectionMatrix;

        protected virtual void LateUpdate()
        {
            if (didUpdateThisFrame && !didUpdatePreviewPixelBufferInCurrentFrame)
                didUpdateThisFrame = false;

            didUpdatePreviewPixelBufferInCurrentFrame = false;
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            if (cameraManager == null || cameraManager.subsystem == null || !cameraManager.subsystem.running)
                return;

            if (!isInitWaiting && !hasInitDone)
                return;


            // Attempt to get the latest camera image. If this method succeeds,
            // it acquires a native resource that must be disposed (see below).
            if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return;
            }


            if (hasInitDone && (previewWidth != image.width || previewHeight != image.height || previewFacingDirection != cameraManager.currentFacingDirection))
            {
                Initialize();
                return;
            }

            bool firstFrame = pixelBufferMat == null;

            int width = image.width;
            int height = image.height;

            if (firstFrame)
            {
                previewWidth = width;
                previewHeight = height;
                previewFacingDirection = cameraManager.currentFacingDirection;

                XRCameraConfiguration config = (XRCameraConfiguration)cameraManager.currentConfiguration;
                previewFramerate = config.framerate.HasValue ? config.framerate.Value : -1;
                pixelBufferMat = new Mat(previewHeight, previewWidth, CvType.CV_8UC4);


                // Remove scaling and offset factors from the camera display matrix while maintaining orientation.
                // Decompose that matrix to extract the rotation and flipping factors.
                // https://github.com/Unity-Technologies/arfoundation-samples/blob/88179bab2b180dd90229d9ec995204be47da1cc1/Assets/Scripts/DisplayDepthImage.cs#L333
                if (eventArgs.displayMatrix.HasValue)
                {
                    // Copy the display rotation matrix from the camera.
                    Matrix4x4 cameraMatrix = eventArgs.displayMatrix ?? Matrix4x4.identity;

                    Vector2 affineBasisX = new Vector2(1.0f, 0.0f);
                    Vector2 affineBasisY = new Vector2(0.0f, 1.0f);
                    Vector2 affineTranslation = new Vector2(0.0f, 0.0f);
#if UNITY_IOS
                    affineBasisX = new Vector2(cameraMatrix[0, 0], cameraMatrix[1, 0]);
                    affineBasisY = new Vector2(cameraMatrix[0, 1], cameraMatrix[1, 1]);
                    affineTranslation = new Vector2(cameraMatrix[2, 0], cameraMatrix[2, 1]);
#endif // UNITY_IOS
#if UNITY_ANDROID
                    affineBasisX = new Vector2(cameraMatrix[0, 0], cameraMatrix[0, 1]);
                    affineBasisY = new Vector2(cameraMatrix[1, 0], cameraMatrix[1, 1]);
                    affineTranslation = new Vector2(cameraMatrix[0, 2], cameraMatrix[1, 2]);
#endif // UNITY_ANDROID

                    affineBasisX = affineBasisX.normalized;
                    affineBasisY = affineBasisY.normalized;
                    Matrix4x4 m_DisplayRotationMatrix = Matrix4x4.identity;
                    m_DisplayRotationMatrix = Matrix4x4.identity;
                    m_DisplayRotationMatrix[0, 0] = affineBasisX.x;
                    m_DisplayRotationMatrix[0, 1] = affineBasisY.x;
                    m_DisplayRotationMatrix[1, 0] = affineBasisX.y;
                    m_DisplayRotationMatrix[1, 1] = affineBasisY.y;

#if UNITY_IOS
                Matrix4x4 FlipYMatrix = Matrix4x4.Scale(new Vector3(1, -1, 1));
                m_DisplayRotationMatrix = FlipYMatrix.inverse * m_DisplayRotationMatrix;
#endif // UNITY_IOS

                    displayRotationAngle = (int)ARUtils.ExtractRotationFromMatrix(ref m_DisplayRotationMatrix).eulerAngles.z;
                    Vector3 localScale = ARUtils.ExtractScaleFromMatrix(ref m_DisplayRotationMatrix);
                    displayFlipVertical = Mathf.Sign(localScale.y) == -1;
                    displayFlipHorizontal = Mathf.Sign(localScale.x) == -1;
                }
            }


            frameEventArgs = eventArgs;

            if (eventArgs.projectionMatrix.HasValue)
            {
                projectionMatrix = (Matrix4x4)eventArgs.projectionMatrix;
            }

            if (cameraManager.TryGetIntrinsics(out var cameraIntrinsics))
            {
                // Rotate the values to match the orientation of the camera image.
                Vector2 fl = cameraIntrinsics.focalLength;
                Vector2 pp = cameraIntrinsics.principalPoint;
                Vector2Int r = cameraIntrinsics.resolution;
                
                Matrix4x4 tM = Matrix4x4.Translate(new Vector3(-r.x / 2, -r.y / 2, 0));
                pp = tM.MultiplyPoint3x4(pp);

                Matrix4x4 displayM = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, displayRotationAngle), new Vector3(displayFlipHorizontal ? -1 : 1, displayFlipVertical ? -1 : 1, 1));
                pp = displayM.MultiplyPoint3x4(pp);

                if (displayRotationAngle == 90 || displayRotationAngle == 270)
                {
                    fl = new Vector2(fl.y, fl.x);
                    r = new Vector2Int(r.y, r.x);
                }

                Matrix4x4 _tM = Matrix4x4.Translate(new Vector3(r.x / 2, r.y / 2, 0));
                pp = _tM.MultiplyPoint3x4(pp);
                
                this.cameraIntrinsics = new XRCameraIntrinsics(fl, pp, r);
            }

            if (eventArgs.timestampNs.HasValue)
            {
                timestampNs = (long)eventArgs.timestampNs;
            }


            bool isRotatedFrame = false;
            if (displayRotationAngle == 90 || displayRotationAngle == 270)
            {
                if (!rotate90Degree)
                    isRotatedFrame = true;
            }
            else if (rotate90Degree)
            {
                isRotatedFrame = true;
            }

            bool _flipVertical = default;
            bool _flipHorizontal = default;
            if (isRotatedFrame)
            {
                if (displayRotationAngle == 90 || displayRotationAngle == 270)
                {
                    // (Orientation is Portrait, rotate90Degree is false)
                    _flipVertical = displayFlipVertical ? !flipHorizontal : flipHorizontal;
                    _flipHorizontal = displayFlipHorizontal ? !flipVertical : flipVertical;
                }
                else
                {
                    // (Orientation is Landscape, rotate90Degrees is true)
                    _flipVertical = displayFlipVertical ? !flipVertical : flipVertical;
                    _flipHorizontal = displayFlipHorizontal ? !flipHorizontal : flipHorizontal;
                }
            }
            else
            {
                if (displayRotationAngle == 90 || displayRotationAngle == 270)
                {
                    // (Orientation is Portrait, rotate90Degree is true)
                    _flipVertical = displayFlipVertical ? flipHorizontal : !flipHorizontal;
                    _flipHorizontal = displayFlipHorizontal ? flipVertical : !flipVertical;
                }
                else
                {
                    // (Orientation is Landscape, rotate90Degrees is false)
                    _flipVertical = displayFlipVertical ? !flipVertical : flipVertical;
                    _flipHorizontal = displayFlipHorizontal ? !flipHorizontal : flipHorizontal;
                }
            }

            int flipCode = calculateFlipCode(_flipVertical, _flipHorizontal);
            XRCpuImage.Transformation transformation = default;
            if (flipCode == int.MinValue)
            {
                transformation = XRCpuImage.Transformation.None;
            }
            else if (flipCode == 0)
            {
                transformation = XRCpuImage.Transformation.MirrorX;
            }
            else if (flipCode == 1)
            {
                transformation = XRCpuImage.Transformation.MirrorY;
            }
            else if (flipCode == -1)
            {
                transformation = XRCpuImage.Transformation.MirrorX | XRCpuImage.Transformation.MirrorY;
            }

            XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32, transformation);
            image.ConvertAsync(conversionParams, ProcessImage);            

            image.Dispose();
        }

        protected virtual void ProcessImage(XRCpuImage.AsyncConversionStatus status, XRCpuImage.ConversionParams conversionParams, NativeArray<byte> data)
        {
            if (status != XRCpuImage.AsyncConversionStatus.Ready)
            {
                Debug.LogErrorFormat("Async request failed with status {0}", status);
                return;
            }

            didUpdateThisFrame = true;
            didUpdatePreviewPixelBufferInCurrentFrame = true; 

            if (hasInitDone)
            {
                MatUtils.copyToMat<byte>(data, pixelBufferMat);

                if (frameMatAcquired != null)
                    frameMatAcquired.Invoke(GetMat(), GetProjectionMatrix(), GetCameraToWorldMatrix(), cameraIntrinsics, timestampNs);
            }
        }

#if !UNITY_EDITOR && !UNITY_ANDROID
        protected bool isScreenSizeChangeWaiting = false;
#endif // !UNITY_EDITOR && !UNITY_ANDROID

        // Update is called once per frame
        protected override void Update()
        {
            if (hasInitDone)
            {
                // Catch the orientation change of the screen and correct the mat image to the correct direction.
                if (screenOrientation != Screen.orientation)
                {
#if !UNITY_EDITOR && !UNITY_ANDROID
                    // Wait one frame until the Screen.width/Screen.height property changes.
                    if (!isScreenSizeChangeWaiting)
                    {
                        isScreenSizeChangeWaiting = true;
                        return;
                    }
                    isScreenSizeChangeWaiting = false;
#endif // !UNITY_EDITOR && !UNITY_ANDROID

                    Initialize();
                }
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        protected override void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Initializes this instance by coroutine.
        /// </summary>
        protected override IEnumerator _Initialize()
        {
            if (hasInitDone)
            {
                ReleaseResources();

                if (onDisposed != null)
                    onDisposed.Invoke();
            }

            isInitWaiting = true;

            // Wait one frame before starting initialization process
            yield return null;


            if (xROrigin == null || xROrigin.Camera == null)
            {
                isInitWaiting = false;
                initCoroutine = null;

                Debug.LogError("XROrigin cannot be null.");

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(Source2MatHelperErrorCode.UNKNOWN, "XROrigin cannot be null.");

                yield break;
            }

            cameraManager = xROrigin.Camera.GetComponent<ARCameraManager>();

            if (cameraManager == null || cameraManager.subsystem == null || !cameraManager.subsystem.running)
            {
                isInitWaiting = false;
                initCoroutine = null;

                Debug.LogError("ARCameraManager is not found.");

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(Source2MatHelperErrorCode.UNKNOWN, "ARCameraManager is not found.");

                yield break;
            }

            // Checks camera permission state.
            if (!cameraManager.permissionGranted)
            {
                IEnumerator coroutine = hasUserAuthorizedCameraPermission();
                yield return coroutine;

                if (!(bool)coroutine.Current)
                {
                    isInitWaiting = false;
                    initCoroutine = null;

                    if (onErrorOccurred != null)
                        onErrorOccurred.Invoke(Source2MatHelperErrorCode.CAMERA_PERMISSION_DENIED, string.Empty);

                    yield break;
                }
            }

            // Sets the camera facing direction.
            CameraFacingDirection newRequestedFacingDirection = requestedIsFrontFacing ? CameraFacingDirection.User : CameraFacingDirection.World;
            if (cameraManager.requestedFacingDirection != newRequestedFacingDirection || cameraManager.currentFacingDirection == CameraFacingDirection.None)
            {
                cameraManager.requestedFacingDirection = newRequestedFacingDirection;
            }

            // Waits the camera facing direction change.
            const int facingDirectionChangeWaitFrameCount =
#if UNITY_IOS
                    30
#else // UNITY_IOS
                    10
#endif // UNITY_IOS
                    ;
            int initFrameCount = 0;

            while (true)
            {
                if (initFrameCount == facingDirectionChangeWaitFrameCount)
                {
                    break;
                }
                else if (initFrameCount >= facingDirectionChangeWaitFrameCount - 5 && newRequestedFacingDirection == cameraManager.currentFacingDirection)
                {
                    break;
                }
                else
                {
                    initFrameCount++;
                    yield return null;
                }
            }


            initFrameCount = 0;
            bool isTimeout = false;

            while (true)
            {
                bool isCreated;
                int configurationsLength;
                using (var configurations = cameraManager.GetConfigurations(Allocator.Temp))
                {
                    isCreated = configurations.IsCreated;
                    configurationsLength = configurations.Length;
                }

                if (initFrameCount > timeoutFrameCount)
                {
                    isTimeout = true;
                    break;
                }
                else if (isCreated && (configurationsLength > 0))
                {
                    break;
                }
                else
                {
                    initFrameCount++;
                    yield return null;
                }
            }

            if (isTimeout)
            {
                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(Source2MatHelperErrorCode.TIMEOUT, string.Empty);
            }

            // Sets the camera resolution and frameRate.
            using (var configurations = cameraManager.GetConfigurations(Allocator.Temp))
            {
                var configuration = cameraManager.currentConfiguration;

                int min1 = configurations.Min(config => Mathf.Abs((config.width * config.height) - (_requestedWidth * _requestedHeight)));
                XRCameraConfiguration matchedResolutionConfig = configurations.First(config => Mathf.Abs((config.width * config.height) - (_requestedWidth * _requestedHeight)) == min1);

                int min2 = int.MaxValue;
                foreach (var config in configurations)
                {
                    if (config.width * config.height == matchedResolutionConfig.width * matchedResolutionConfig.height)
                    {
                        int framerate = config.framerate.HasValue ? config.framerate.Value : 0;
                        int abs = Mathf.Abs(framerate - (int)_requestedFPS);

                        if (abs < min2)
                        {
                            min2 = abs;
                            configuration = config;
                        }
                    }
                }

                if (!cameraManager.currentConfiguration.Equals(configuration))
                    cameraManager.currentConfiguration = configuration;
            }

            // Waits the camera resolution change.
            const int resolutionChangeWaitFrameCount =
#if UNITY_IOS
                    30
#else // UNITY_IOS
                    5
#endif // UNITY_IOS
                    ;
            initFrameCount = 0;

            while (true)
            {
                if (initFrameCount == resolutionChangeWaitFrameCount)
                {
                    break;
                }
                else
                {
                    initFrameCount++;
                    yield return null;
                }
            }


            isPlaying = true;
            cameraManager.frameReceived += OnCameraFrameReceived;

            while (true)
            {
                if (initFrameCount > timeoutFrameCount)
                {
                    isTimeout = true;
                    break;
                }
                else if (didUpdateThisFrame)
                {

                    Debug.Log("ARFoundationCamera2MatHelper:: " + "UniqueID:" + cameraManager.name + " width:" + previewWidth + " height:" + previewHeight + " fps:" + previewFramerate
                    + " isFrongFacing:" + (cameraManager.currentFacingDirection == CameraFacingDirection.User));

                    baseMat = new Mat(previewHeight, previewWidth, CvType.CV_8UC4);

                    if (baseColorFormat == outputColorFormat)
                    {
                        frameMat = baseMat;
                    }
                    else
                    {
                        frameMat = new Mat(baseMat.rows(), baseMat.cols(), CvType.CV_8UC(Source2MatHelperUtils.Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));
                    }

                    screenOrientation = Screen.orientation;
                    
                    bool isRotatedFrame = false;
                    if (displayRotationAngle == 90 || displayRotationAngle == 270)
                    {
                        if (!rotate90Degree)
                            isRotatedFrame = true;
                    }
                    else if (rotate90Degree)
                    {
                        isRotatedFrame = true;
                    }

                    if (isRotatedFrame)
                        rotatedFrameMat = new Mat(frameMat.cols(), frameMat.rows(), CvType.CV_8UC(Source2MatHelperUtils.Channels(outputColorFormat)), new Scalar(0, 0, 0, 255));
                    
                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;

                    if (onInitialized != null)
                        onInitialized.Invoke();

                    break;
                }
                else
                {
                    initFrameCount++;
                    yield return null;
                }

            }

            if (isTimeout)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;

                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(Source2MatHelperErrorCode.TIMEOUT, string.Empty);
            }
        }

        /// <summary>
        /// Starts the camera.
        /// </summary>
        public override void Play()
        {
            if (cameraManager == null || cameraManager.subsystem == null || !cameraManager.subsystem.running) return;

            if (hasInitDone && !isPlaying)
            {
                cameraManager.frameReceived += OnCameraFrameReceived;
                isPlaying = true;
            }
        }

        /// <summary>
        /// Pauses the active camera.
        /// </summary>
        public override void Pause()
        {
            Stop();
        }

        /// <summary>
        /// Stops the active camera.
        /// </summary>
        public override void Stop()
        {
            if (cameraManager == null || cameraManager.subsystem == null || !cameraManager.subsystem.running) return;

            if (hasInitDone && isPlaying)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
                isPlaying = false;
                didUpdateThisFrame = false;
            }
        }

        /// <summary>
        /// Indicates whether the active camera is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the active camera is playing, <c>false</c> otherwise.</returns>
        public override bool IsPlaying()
        {
            return isPlaying;
        }

        /// <summary>
        /// Indicates whether the active camera device is currently front facng.
        /// </summary>
        /// <returns><c>true</c>, if the active camera device is front facng, <c>false</c> otherwise.</returns>
        public override bool IsFrontFacing()
        {
            if (cameraManager == null || cameraManager.subsystem == null || !cameraManager.subsystem.running) return false;

            return (cameraManager.currentFacingDirection == CameraFacingDirection.User);
        }

        /// <summary>
        /// Returns the active camera device name.
        /// </summary>
        /// <returns>The active camera device name.</returns>
        public override string GetDeviceName()
        {
            if (cameraManager == null || cameraManager.subsystem == null || !cameraManager.subsystem.running) return "";

            return cameraManager.name;
        }

        /// <summary>
        /// Returns the active camera framerate.
        /// </summary>
        /// <returns>The active camera framerate.</returns>
        public override float GetFPS()
        {
            return previewFramerate;
        }

        /// <summary>
        /// Returns the active WebcamTexture.
        /// </summary>
        /// <returns>The active WebcamTexture.</returns>
        public override WebCamTexture GetWebCamTexture()
        {
            return null;
        }

        /// <summary>
        /// Returns the camera to world matrix.
        /// </summary>
        /// <returns>The camera to world matrix.</returns>
        public override Matrix4x4 GetCameraToWorldMatrix()
        {
            return (hasInitDone && xROrigin != null && xROrigin.Camera != null) ? xROrigin.Camera.cameraToWorldMatrix : Matrix4x4.identity;
        }

        /// <summary>
        /// Returns the projection matrix matrix.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public override Matrix4x4 GetProjectionMatrix()
        {
            return (hasInitDone) ? projectionMatrix : Matrix4x4.identity;
        }

        /// <summary>
        /// Indicates whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public override bool DidUpdateThisFrame()
        {
            if (!hasInitDone)
                return false;

            return didUpdateThisFrame;
        }

        /// <summary>
        /// Gets the mat of the current frame.
        /// The Mat object's type is 'CV_8UC4' or 'CV_8UC3' or 'CV_8UC1' (ColorFormat is determined by the outputColorFormat setting).
        /// Please do not dispose of the returned mat as it will be reused.
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public override Mat GetMat()
        {
            if (!hasInitDone || cameraManager == null || cameraManager.subsystem == null || !cameraManager.subsystem.running || pixelBufferMat == null)
            {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }

            if (baseColorFormat == outputColorFormat)
            {
                pixelBufferMat.copyTo(frameMat);
            }
            else
            {
                pixelBufferMat.copyTo(baseMat);
                Imgproc.cvtColor(baseMat, frameMat, Source2MatHelperUtils.ColorConversionCodes(baseColorFormat, outputColorFormat));
            }

            if (rotatedFrameMat != null)
            {
                Core.rotate(frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                return rotatedFrameMat;
            }
            else
            {
                return frameMat;
            }
        }

        protected virtual int calculateFlipCode(bool flipVertical, bool flipHorizontal)
        {
            int flipCode = int.MinValue;

            if (displayRotationAngle == 180 || displayRotationAngle == 270)
            {
                flipCode = -1;
            }

            if (flipVertical)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 0;
                }
                else if (flipCode == 0)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == 1)
                {
                    flipCode = -1;
                }
                else if (flipCode == -1)
                {
                    flipCode = 1;
                }
            }

            if (flipHorizontal)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 1;
                }
                else if (flipCode == 0)
                {
                    flipCode = -1;
                }
                else if (flipCode == 1)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == -1)
                {
                    flipCode = 0;
                }
            }

            return flipCode;
        }

        /// <summary>
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        protected void FlipMat(Mat mat, bool flipVertical, bool flipHorizontal)
        {
            int flipCode = int.MinValue;

            if (displayRotationAngle == 180 || displayRotationAngle == 270)
            {
                flipCode = -1;
            }

            if (flipVertical)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 0;
                }
                else if (flipCode == 0)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == 1)
                {
                    flipCode = -1;
                }
                else if (flipCode == -1)
                {
                    flipCode = 1;
                }
            }

            if (flipHorizontal)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 1;
                }
                else if (flipCode == 0)
                {
                    flipCode = -1;
                }
                else if (flipCode == 1)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == -1)
                {
                    flipCode = 0;
                }
            }

            if (flipCode > int.MinValue)
            {
                Core.flip(mat, mat, flipCode);
            }
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected override void ReleaseResources()
        {
            isInitWaiting = false;
            hasInitDone = false;


            if (cameraManager != null && isPlaying)
            {
                cameraManager.frameReceived -= OnCameraFrameReceived;
                isPlaying = false;
            }

            if (pixelBufferMat != null)
            {
                pixelBufferMat.Dispose();
                pixelBufferMat = null;
            }
            previewWidth = default;
            previewHeight = default;
            previewFramerate = -1;
            previewFacingDirection = CameraFacingDirection.None;

            projectionMatrix = default;
            frameEventArgs = default;

            didUpdateThisFrame = false;
            didUpdatePreviewPixelBufferInCurrentFrame = false;


            if (frameMat != null)
            {
                frameMat.Dispose();
                frameMat = null;
            }
            if (baseMat != null)
            {
                baseMat.Dispose();
                baseMat = null;
            }
            if (rotatedFrameMat != null)
            {
                rotatedFrameMat.Dispose();
                rotatedFrameMat = null;
            }
        }

        /// <summary>
        /// Releases all resource used by the <see cref="ARFoundationCamera2MatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="ARFoundationCamera2MatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="ARFoundationCamera2MatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="ARFoundationCamera2MatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="ARFoundationCamera2MatHelper"/> was occupying.</remarks>
        public override void Dispose()
        {
            if (colors != null)
                colors = null;

            if (isInitWaiting)
            {
                CancelInitCoroutine();

                ReleaseResources();
            }
            else if (hasInitDone)
            {
                ReleaseResources();

                if (onDisposed != null)
                    onDisposed.Invoke();
            }
        }

#endif // (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR && !DISABLE_ARFOUNDATION_API

    }
}
