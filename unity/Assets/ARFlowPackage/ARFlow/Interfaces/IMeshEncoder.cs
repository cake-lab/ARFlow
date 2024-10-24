using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ARFlow
{
    public interface IMeshEncoder
    {
        public List<NativeArray<byte>> EncodeMesh(Mesh mesh);
    }

}
