using UnityEngine;

namespace CakeLab.ARFlow.Evaluation
{
    /// <summary>
    /// Class to evaluate the spacial accuracy of the ARFlow system.
    /// 

    public class SpacialEvaluation
    {
        /// <summary>
        /// Calculate the Frobenius norm of the difference between the ground truth and the obtained pose
        /// Matrix4x4 does not have a built-in matrix operation for adding, so we will manually calculate the Frobenius norm
        /// </summary>
        /// <param name="groundTruth">Ground truth pose data in transformation matrix</param>
        /// <param name="obtainedPose">Predicted pose data from the current method</param>
        /// <returns></returns>
        // public abstract Matrix4x4 ObtainPoseFromImage(Texture2D capturedImage, float markerLength, Mat camIntrinsics);
        public static float GetErrorScore(Matrix4x4 groundTruth, Matrix4x4 obtainedPose)
        {
            float errorScore = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    float difference = groundTruth[i, j] - obtainedPose[i, j];
                    errorScore += difference * difference;
                }
            }

            return Mathf.Sqrt(errorScore);
        }
    }
}
