using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace CakeLab.ARFlow.Utilities
{
    public static class MathUtilities 
    {
        public static Quaternion ExtractRotationFromMatrix(Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            return Quaternion.LookRotation(forward, upwards);
        }

        public static Vector3 ExtractPositionFromMatrix(Matrix4x4 matrix)
        {
            Vector3 position;
            position.x = matrix.m03;
            position.y = matrix.m13;
            position.z = matrix.m23;
            return position;
        }

        /// <summary>
        /// Compute the error of 2 quaternion based on their offset
        /// </summary>
        /// <returns>The floating point error</returns>
        public static float ComputeQuaternionError(Quaternion a, Quaternion b) {
            Quaternion offset = Quaternion.Inverse(a) * b;

            float angle;
            Vector3 axis;
            offset.ToAngleAxis(out angle, out axis);

            // Ensure the angle is within [0, 180] degrees
            if (angle > 180f)
                angle = 360f - angle; 

            return angle; // Angular error in degrees
        }

        /// <summary>
        /// Calculate the L2 error
        /// </summary>
        /// <param name="groundTruth"></param>
        /// <param name="predictedPose"></param>
        /// <returns></returns>
        public static float GetTranslationError(Matrix4x4 groundTruth, Matrix4x4 predictedPose)
        {
            Vector3 truth = ExtractPositionFromMatrix(groundTruth);
            Vector3 pred = ExtractPositionFromMatrix(groundTruth);
            return Mathf.Sqrt(Vector3.Distance(truth, pred));
        }

        /// <summary>
        /// Calculate the error in degrees
        /// </summary>
        /// <param name="groundTruth"></param>
        /// <param name="predictedPose"></param>
        /// <returns></returns>
        public static float GetRotationalError(Matrix4x4 groundTruth, Matrix4x4 predictedPose)
        {
            Quaternion truth = ExtractRotationFromMatrix(groundTruth);
            Quaternion pred = ExtractRotationFromMatrix(predictedPose);

            return ComputeQuaternionError(truth, pred);
        }

        /// <summary>
        /// Calculate the Frobenius norm of the difference between 2 Matrix4x4
        /// Matrix4x4 does not have a built-in matrix operation for adding, so we will manually calculate this
        /// </summary>
        /// <param name="groundTruth">Ground truth pose data in transformation matrix</param>
        /// <param name="predictedPose">Predicted pose data from the current method</param>
        /// <returns></returns>
        public static float GetFrobeniusNorm(Matrix4x4 groundTruth, Matrix4x4 predictedPose)
        {
            float errorScore = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    float difference = groundTruth[i, j] - predictedPose[i, j];
                    errorScore += difference * difference;
                }
            }

            return Mathf.Sqrt(errorScore);
        }
    }
}
