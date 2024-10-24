using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Draco.Encoder;
using Unity.Collections;

namespace ARFlow
{
    public class MeshEncoder
    {
        public List<NativeArray<byte>> EncodeMesh(Mesh mesh)
        {
            EncodeResult[] result = DracoEncoder.EncodeMesh(mesh);
            List<NativeArray<byte>> ret = new();
            foreach (EncodeResult item in result)
            {
                ret.Add(item.data);
            }
            return ret;
        }
    }

}

