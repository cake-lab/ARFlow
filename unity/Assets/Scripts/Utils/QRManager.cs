using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ZXing;
using ZXing.Common;
using ZXing.QrCode;

using System;

using ARFlow;
using Unity.VisualScripting;

public class QRManager
{
    private static QRManager instance;
    private static readonly object padlock;
    public static QRManager Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new QRManager();
                }
                return instance;
            }
        }
    }
    Texture2D lastTexture;
    QRManager()
    {

    }
    public string readQRCode(BinaryBitmap image)
    {
        var qrReader = new QRCodeReader();
        var res = "";
        try
        {
            var result = qrReader.decode(image);
            if (result != null)
            {
                res = result.Text;
            }
        }
        catch (Exception e)
        {
            OtherUtils.PrintDebug(e);
        }

        return res;
    }

    public Texture2D encode(string toEncode, int width = 512, int height = 512)
    {
        var writer = new QRCodeWriter();
        BitMatrix imageMat = writer.encode(toEncode, BarcodeFormat.QR_CODE, width, height);

        Texture2D encodeRes = new Texture2D(width, height,TextureFormat.RGBA32, false);
        Color[] pixels = encodeRes.GetPixels();

        int k = 0;
        //borrowed from this: https://gist.github.com/AngeloYazar/6101673
        for (int j = 0; j < 512; j++)
        {
            ZXing.Common.BitArray row = new ZXing.Common.BitArray(512);
            row = imageMat.getRow(j, null);
            int[] intRow = row.Array;
            for (int i = intRow.Length - 1; i >= 0; i--)
            {
                int thirtyTwoPixels = intRow[i];
                for (int b = 31; b >= 0; b--)
                {
                    int pixel = ((thirtyTwoPixels >> b) & 1);
                    if (pixel == 0)
                    {
                        pixels[k] = Color.white;
                    }
                    else
                    {
                        pixels[k] = Color.black;
                    }
                    k++;
                }
            }
        }

        encodeRes.SetPixels(pixels);
        encodeRes.Apply();

        lastTexture = encodeRes;

        return encodeRes;
    }
}
