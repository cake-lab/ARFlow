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
        public override Matrix4x4 ObtainPoseFromImage(Texture2D capturedImage, float markerLength, Mat camIntrinsics)
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

                        // // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        // Calib3d.drawFrameAxes(rgbMat, _camIntrinsics, _distCoeffs, rvec, tvec, markerLength * 0.5f);

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
            PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

            var res = ARUtils.ConvertPoseDataToMatrix(ref poseData, true);

            //Through testing, it is observed that the change in coordinate system is missing for the rotational data. 
            //Perform coordinate change for rotational data
            res = FlipAxes(res);
            
            return res;
        }

        private Matrix4x4 FlipAxes(Matrix4x4 mat){
            
            Vector4 col0 = mat.GetColumn(0);
            Vector4 col1 = mat.GetColumn(1);
            Vector4 col2 = mat.GetColumn(2);

            Matrix4x4 corrected = mat;
            corrected.SetColumn(0, col0); // Right
            corrected.SetColumn(1, col2); // Up
            corrected.SetColumn(2, -col1); // Forward

            return corrected;
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
}
