using System.Collections.Generic;
using System.Threading.Tasks;
using Draco.Encode;
using Unity.Collections;
using UnityEngine;

namespace CakeLab.ARFlow.Utilties
{
    public class MeshEncoder
    {
        public async Task<List<NativeArray<byte>>> EncodeMeshAsync(Mesh mesh)
        {
            EncodeResult[] result = await DracoEncoder.EncodeMesh(mesh);
            List<NativeArray<byte>> ret = new();
            foreach (EncodeResult item in result)
            {
                ret.Add(item.data);
            }
            return ret;
        }

        public List<NativeArray<byte>> EncodeMesh(Mesh mesh)
        {
            return EncodeMeshAsync(mesh).Result;
        }
    }
}
