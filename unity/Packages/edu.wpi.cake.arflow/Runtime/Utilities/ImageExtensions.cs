using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace CakeLab.ARFlow.Utilities
{
    /// <summary>
    /// Interface for encoding image
    /// </summary>
    internal interface IImageEncodable : IDisposable
    {
        public byte[] Encode();
    }

    /// <summary>
    /// Depth image information with confidence
    /// </summary>
    internal struct ConfidenceFilteredDepthImage : IImageEncodable
    {
        private XRCpuImage _temporalSmoothedDepthImage;
        private XRCpuImage _environmentDepthConfidenceImage;
        private readonly int _minConfidence;

        public readonly Vector2Int Size()
        {
            return _temporalSmoothedDepthImage.dimensions;
        }

        /// <summary>
        /// Filter depth image data with confidence image. The two images should be acquired at the same time. Should be obtained from <see cref="AROcclusionManager"/>.
        /// </summary>
        /// <param name="minConfidence">Min confidence for filtering</param>
        public ConfidenceFilteredDepthImage(
            XRCpuImage temporalSmoothedDepthImage,
            XRCpuImage environmentDepthConfidenceImage,
            int minConfidence = 1
        )
        {
            _temporalSmoothedDepthImage = temporalSmoothedDepthImage;
            _environmentDepthConfidenceImage = environmentDepthConfidenceImage;
            _minConfidence = minConfidence;
        }

        /// <summary>
        /// For each depth value, if confidence is lower than minConfidence, it will be ignored (replaced with 0s).
        /// The rest is encoded to bytes.
        /// </summary>
        /// <returns>Encoded bytes</returns>
        public byte[] Encode()
        {
            var depthValues = _temporalSmoothedDepthImage.GetPlane(0).data.ToArray();
            var confidenceValues = _environmentDepthConfidenceImage.GetPlane(0).data;

            for (var i = 0; i < confidenceValues.Length; i++)
            {
                // filter low confidence depth
                // convert to 1000, will be occluded by later calculation on edge
                var c = confidenceValues[i];
                if (c >= _minConfidence)
                    continue;

                var dataLength =
                    _temporalSmoothedDepthImage.format == XRCpuImage.Format.DepthFloat32 ? 4 : 2;

                for (var j = 0; j < dataLength; j++)
                {
                    // Replacing filtered depth with 0.
                    depthValues[i * dataLength + j] = 0;
                    depthValues[i * dataLength + j] = 0;
                    depthValues[i * dataLength + j] = 0;
                    depthValues[i * dataLength + j] = 0;
                }
            }

            return depthValues;
        }

        public void Dispose()
        {
            _temporalSmoothedDepthImage.Dispose();
            _environmentDepthConfidenceImage.Dispose();
        }
    }

    /// <summary>
    /// Color image information
    /// </summary>
    public struct YCbCrColorImage : IImageEncodable
    {
        private XRCpuImage _image;
        private readonly float _scale;
        private readonly Vector2Int _nativeSize;
        private readonly Vector2Int _sampleSize;

        /// <summary>
        /// Set scale of raw data image to relative of sample (depth) size.
        /// </summary>
        /// <param name="cameraImage">Raw color image. Obtained from <see cref="ARCameraManager"/>.</param>
        /// <param name="sampleSize">Sample size/dimensions of the depth image. Obtained from non-smoothed environment depth image in <see cref="AROcclusionManager"/>.</param>
        public YCbCrColorImage(XRCpuImage cameraImage, Vector2Int sampleSize)
        {
            _image = cameraImage;
            _nativeSize = cameraImage.dimensions;
            _sampleSize = sampleSize;
            _scale = _sampleSize.x / (float)_nativeSize.x;
        }

        /// <summary>
        /// Resample color image to right size and convert to bytes.
        /// </summary>
        /// <returns>Encoded bytes</returns>
        public byte[] Encode()
        {
            var size = _sampleSize.x * _sampleSize.y + 2 * (_sampleSize.x / 2 * _sampleSize.y / 2);
            var colorBytes = new byte[size];

            // Currently using nearest sampling, consider upgrade
            // to bi-linear sampling for better anti-aliasing.
            var planeY = _image.GetPlane(0).data;
            for (var v = 0; v < _sampleSize.y; v++)
            {
                for (var u = 0; u < _sampleSize.x; u++)
                {
                    var iv = (int)(v / _scale);
                    var iu = (int)(u / _scale);
                    colorBytes[v * _sampleSize.x + u] = planeY[iv * _nativeSize.x + iu];
                }
            }

            var planeCbCr = _image.GetPlane(1).data;
            var offsetUV = _sampleSize.x * _sampleSize.y;
            for (var v = 0; v < _sampleSize.y / 2; v++)
            {
                for (var u = 0; u < _sampleSize.x / 2; u++)
                {
                    var iv = (int)(v / _scale);
                    var iu = (int)(u / _scale);

                    var sampleOffset = offsetUV + v * _sampleSize.x + u * 2;
                    var nativeOffset = iv * _nativeSize.x / 2 * 2 + iu * 2;

                    colorBytes[sampleOffset + 0] = planeCbCr[nativeOffset + 0];
                    colorBytes[sampleOffset + 1] = planeCbCr[nativeOffset + 1];
                }
            }

            return colorBytes;
        }

        public void Dispose()
        {
            _image.Dispose();
        }
    }
}
