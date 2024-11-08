using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.Client.Result;

using System;

using ARFlow;
using Unity.VisualScripting;

public static class QRManager
{
    private static readonly object padlock;

    /// <summary>
    /// Read QR from image bytes, assuming image is in RGBA32 format.
    /// </summary>
    /// <returns></returns>
    public static string readQRCode(byte[] image, int width, int height)
    {

        var reader = new BarcodeReaderGeneric();

        var res = "";
        try
        {

            //BinaryBitmap bitmap = new BinaryBitmap(new HybridBinarizer(new RGBLuminanceSource(image, width, height)));
            var result = reader.Decode(image, width, height, RGBLuminanceSource.BitmapFormat.RGBA32);

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

    public static Texture2D encode(string toEncode, int width = 512, int height = 512)
    {
        

        var writer = new QRCodeWriter();
        BitMatrix imageMat = writer.encode(toEncode, BarcodeFormat.QR_CODE, width, height);

        return BitMatToTexture2D(imageMat, width, height);
    }

    private static Texture2D BitMatToTexture2D (BitMatrix imageMat, int width, int height)
    {
        Texture2D encodeRes = new Texture2D(width, height, TextureFormat.RGBA32, false);
        encodeRes.hideFlags = HideFlags.DontSave;
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


        return encodeRes;
    }
}
