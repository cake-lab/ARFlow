using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Draco.Encoder;
using Unity.Collections;

namespace ARFlow
{
    public class MeshingEncoder
    {
        public static List<NativeArray<byte>> encodeMesh(Mesh mesh)
        {
            EncodeResult[] result = DracoEncoder.EncodeMesh(mesh);
            List<NativeArray<byte>> ret = new List<NativeArray<byte>>(); 
            foreach (EncodeResult item in result)
            {
                ret.Add(item.data);
            }
            return ret;
        }
    }
}
