using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace Unity.AR.Companion.Mobile
{
    class CutoutMaskUI : Image
    {
        static readonly int k_StencilComp = Shader.PropertyToID("_StencilComp");

        public override Material materialForRendering
        {
            get
            {
                var renderingMaterial = new Material(base.materialForRendering);
                renderingMaterial.SetInt(k_StencilComp, (int)CompareFunction.NotEqual);
                return renderingMaterial;
            }
        }
    }
}
