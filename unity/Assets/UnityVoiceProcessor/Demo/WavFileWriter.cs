//
// Copyright 2023 Picovoice Inc.
//
// You may not use this file except in compliance with the license. A copy of the license is located in the "LICENSE"
// file accompanying this source.
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
// an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
// specific language governing permissions and limitations under the License.
//

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Pv.Unity
{
    class WavFileWriter
    {
        public void Save(string fileName, List<short[]> audioData)
        {
            if (audioData.Count == 0)
            {
                return;
            }

            short[] samples = new short[audioData.Count * audioData[0].Length];
            for (var i = 0; i < audioData.Count; i++)
            {
                audioData[i].CopyTo(samples, i * audioData[0].Length);
            }

            var filePath = Path.Combine(Application.persistentDataPath + "/", fileName);
            Debug.Log(filePath);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                _writeWavHeader(fileStream, samples);

                byte[] byteData = new byte[samples.Length * 2];
                Buffer.BlockCopy(samples, 0, byteData, 0, samples.Length * 2);

                fileStream.Write(byteData, 0, byteData.Length);
            }
        }

        private void _writeWavHeader(FileStream fileStream, short[] samples)
        {
            var sampleRate = 16000;
            var channels = 1;

            fileStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            Byte[] audioFormat = BitConverter.GetBytes(1);
            fileStream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            Byte[] sampleRateBytes = BitConverter.GetBytes(sampleRate);
            fileStream.Write(sampleRateBytes, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(sampleRate * channels * 2);
            fileStream.Write(byteRate, 0, 4);

            UInt16 blockAlign = (ushort)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            UInt16 bps = 16;
            Byte[] bitsPerSample = BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(dataString, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(samples.Length * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
        }
    }
}
