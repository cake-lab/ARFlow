using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QRCodeDisplay : MonoBehaviour
{
    public RawImage rawImage;
    public void DisplayQRFromString(string str)
    {
        Texture2D tex = QRManager.Instance.encode(str);

        rawImage.texture = tex;
    }
}
