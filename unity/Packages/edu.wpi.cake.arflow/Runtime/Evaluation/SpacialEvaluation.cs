using UnityEngine;
using System.IO;
using System;

using static CakeLab.ARFlow.Utilities.MathUtilities;
using JetBrains.Annotations;
using Mono.Cecil;
using PlasticGui.WorkspaceWindow.Configuration;
using OpenCVForUnity.CoreModule;

namespace CakeLab.ARFlow.Evaluation
{
    public struct EvalParam
    {
        public String deviceModel;
        public String deviceName;
        public DeviceType deviceType;
        public int totalCount;
    }

    public struct EvalCameraIntrinsics
    {
        public Vector2 focalLength;
        public Vector2 principalPoint;
    }
    public struct Vector3Setup 
    {
        public Vector2 xVariation;
        public Vector2 yVariation;
        public Vector2 zVariation;
    }
    public struct EvalSetupInfo
    {

        /// <summary>
        /// Range is in viewport coordinates (x and y is 0 to 1)
        /// </summary>
        public Vector3Setup positionSetup;

        /// <summary>
        /// Range is in euler angles
        /// </summary>
        public Vector3Setup rotationSetup;
    }
    public struct EvalInfo 
    {
        public EvalParam evalParam;
        public EvalCameraIntrinsics cameraIntrinsics;

        public EvalSetupInfo setupInfo;
        /// <summary>
        /// Total time to run the prediction, even if prediction does not yield result
        /// </summary>
        public float totalTime;
        public float matrixError;
        public float translationError;
        public float rotationError;
        public int validScanCount;
    }

    /// <summary>
    /// Class to evaluate the spacial accuracy of the ARFlow system.
    /// </summary>
    public abstract class SpacialEvaluation
    {

        private string GetCurrentTime() 
        {
            return DateTime.UtcNow.ToString("yyMMdd'T'HHmmss");
        }

        /// <summary>
        /// The default path for evaluations/benchmarkings is saved in unity/Benchmark
        /// </summary>
        /// <returns>Save path as string</returns>
        private string CreateAndGetSaveFolder() 
        {
            string path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
            // string path = Application.dataPath;
            path = Path.Combine(path, "Benchmark", $"{GetCurrentTime()}");
            var inf = Directory.CreateDirectory(path);
            Debug.Log(inf);
            return path;
        }

        public void WriteResultToFile(EvalInfo info) {
            string fileName = "Result.txt";
            string path = Path.Combine(CreateAndGetSaveFolder(), fileName);

            using (StreamWriter writer = new StreamWriter(path, false)) 
            {
                writer.WriteLine($"---Parameters---");
                writer.WriteLine($"Device model: {info.evalParam.deviceModel}");
                writer.WriteLine($"Device name: {info.evalParam.deviceName}");
                writer.WriteLine($"Device type: {info.evalParam.deviceType}");
                writer.WriteLine($"Total iteration count: {info.evalParam.totalCount}");
                writer.WriteLine();

                writer.WriteLine($"---Camera intrinsics---");
                writer.WriteLine($"Focal length: ({info.cameraIntrinsics.focalLength.x}, {info.cameraIntrinsics.focalLength.y})");
                writer.WriteLine($"Principal point: ({info.cameraIntrinsics.principalPoint.x}, {info.cameraIntrinsics.principalPoint.y})");

                writer.WriteLine();
                writer.WriteLine("---Setup info---");
                writer.WriteLine($"Position randomization: ");
                writer.WriteLine($"- Viewport x (from 0-1): {info.setupInfo.positionSetup.xVariation.x} - {info.setupInfo.positionSetup.xVariation.y} ");
                writer.WriteLine($"- Viewport y: {info.setupInfo.positionSetup.yVariation.x} - {info.setupInfo.positionSetup.yVariation.y} ");
                writer.WriteLine($"- z coordinate: {info.setupInfo.positionSetup.zVariation.x} - {info.setupInfo.positionSetup.zVariation.y} ");
                writer.WriteLine($"Rotation randomization (in euler angles): ");
                writer.WriteLine($"- x: {info.setupInfo.rotationSetup.xVariation.x} - {info.setupInfo.rotationSetup.xVariation.y} ");
                writer.WriteLine($"- y: {info.setupInfo.rotationSetup.yVariation.x} - {info.setupInfo.rotationSetup.yVariation.y} ");
                writer.WriteLine($"- z: {info.setupInfo.rotationSetup.zVariation.x} - {info.setupInfo.rotationSetup.zVariation.y} ");

                writer.WriteLine();
                writer.WriteLine("---Evaluation info---");
                writer.WriteLine($"Total time for all predictions: {info.totalTime}");
                writer.WriteLine($"TRS Matrix error - L2: {info.matrixError}");
                writer.WriteLine($"Translation error - L2: {info.translationError}");
                writer.WriteLine($"Rotation error (degree): {info.rotationError}");
                writer.WriteLine($"Scanned count: {info.validScanCount}");

                writer.Close();
            }

            Debug.Log("done");
        }

        public abstract Matrix4x4 ObtainPoseFromImage(Texture2D screenShot, float markerLength, Mat intrinsics);
        
    }
}
