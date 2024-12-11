namespace CakeLab.ARFlow.DataBuffers
{
    public struct RawARPlane
    {
        public int rowStride;
        public int pixelStride;
        public byte[] data;

        public static explicit operator RawARPlane(UnityEngine.XR.ARSubsystems.XRCpuImage.Plane plane)
        {
            var rawPlane = new RawARPlane
            {
                rowStride = plane.rowStride,
                pixelStride = plane.pixelStride,
                data = plane.data.ToArray(),
            };
            return rawPlane;
        }


        public static explicit operator Grpc.V1.XRCpuImage.Types.Plane(RawARPlane plane)
        {
            var grpcPlane = new Grpc.V1.XRCpuImage.Types.Plane
            {
                RowStride = plane.rowStride,
                PixelStride = plane.pixelStride,
                Data = Google.Protobuf.ByteString.CopyFrom(plane.data),
            };
            return grpcPlane;
        }
    }
}
