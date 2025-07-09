using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Google.Protobuf;
using System.Threading.Tasks;

namespace ARFlow
{
    
    /// Handles on-device storage of AR data frames in binary format

    public class ARFlowDataStorage
    {
        private readonly string _storagePath;
        private readonly Queue<string> _fileQueue;
        private readonly int _maxStoredFiles;
        private int _frameCounter = 0;

        public ARFlowDataStorage(int maxStoredFiles = 2000)
        {
            _maxStoredFiles = maxStoredFiles;
            _fileQueue = new Queue<string>();

            // Create storage directory in persistent data path
            _storagePath = Path.Combine(Application.persistentDataPath, "ARFlowData");
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            Debug.Log($"ARFlow storage initialized at: {_storagePath}");
        }

        
        /// Store a data frame to local storage
   
        public async Task<string> StoreFrameAsync(DataFrameRequest frameData, Dictionary<string, object> metadata = null)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            var fileName = $"frame_{_frameCounter:D6}_{timestamp}.bin";
            var filePath = Path.Combine(_storagePath, fileName);

            try
            {
                using var fileStream = File.Create(filePath);

                // Write metadata size and metadata
                var metadataJson = metadata != null ? JsonUtility.ToJson(new SerializableDictionary(metadata)) : "{}";
                var metadataBytes = System.Text.Encoding.UTF8.GetBytes(metadataJson);
                var metadataSizeBytes = BitConverter.GetBytes(metadataBytes.Length);

                await fileStream.WriteAsync(metadataSizeBytes, 0, metadataSizeBytes.Length);
                await fileStream.WriteAsync(metadataBytes, 0, metadataBytes.Length);

                // Write frame data
                var frameBytes = frameData.ToByteArray();
                await fileStream.WriteAsync(frameBytes, 0, frameBytes.Length);

                _fileQueue.Enqueue(filePath);
                _frameCounter++;

                // Manage storage limit
                if (_fileQueue.Count > _maxStoredFiles)
                {
                    var oldFile = _fileQueue.Dequeue();
                    if (File.Exists(oldFile))
                    {
                        File.Delete(oldFile);
                    }
                }

                Debug.Log($"Frame stored: {Path.GetFileName(filePath)}");
                return filePath;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to store frame: {e.Message}");
                return null;
            }
        }

        
        /// Store frame data 
       
        public string StoreFrame(DataFrameRequest frameData, Dictionary<string, object> metadata = null)
        {
            return StoreFrameAsync(frameData, metadata).Result;
        }

        
        /// Load a stored frame by file path

        public async Task<(DataFrameRequest frameData, Dictionary<string, object> metadata)> LoadFrameAsync(string filePath)
        {
            try
            {
                using var fileStream = File.OpenRead(filePath);

                // Read metadata size
                var metadataSizeBytes = new byte[4];
                await fileStream.ReadAsync(metadataSizeBytes, 0, 4);
                var metadataSize = BitConverter.ToInt32(metadataSizeBytes, 0);

                // Read metadata
                var metadataBytes = new byte[metadataSize];
                await fileStream.ReadAsync(metadataBytes, 0, metadataSize);
                var metadataJson = System.Text.Encoding.UTF8.GetString(metadataBytes);
                var metadata = JsonUtility.FromJson<SerializableDictionary>(metadataJson)?.ToDictionary();

                // Read frame data
                var frameDataLength = (int)(fileStream.Length - fileStream.Position);
                var frameBytes = new byte[frameDataLength];
                await fileStream.ReadAsync(frameBytes, 0, frameDataLength);

                var frameData = DataFrameRequest.Parser.ParseFrom(frameBytes);
                return (frameData, metadata);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load frame from {filePath}: {e.Message}");
                return (null, null);
            }
        }

        
        /// Get list of all stored frame files
 
        public List<string> GetStoredFrames()
        {
            var files = new List<string>();
            try
            {
                files.AddRange(Directory.GetFiles(_storagePath, "*.bin"));
                files.Sort(); // Sort by filename (includes timestamp)
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get stored frames: {e.Message}");
            }
            return files;
        }

        
        /// Clear all stored data
       
        public void ClearStorage()
        {
           
                var files = Directory.GetFiles(_storagePath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                _fileQueue.Clear();
                _frameCounter = 0;
                Debug.Log("Storage cleared");
            
       
        }

        
        /// Get storage statistics
    
        public (int fileCount, long totalSizeBytes, string storagePath) GetStorageInfo()
        {
            try
            {
                var files = Directory.GetFiles(_storagePath);
                long totalSize = 0;
                foreach (var file in files)
                {
                    totalSize += new FileInfo(file).Length;
                }
                return (files.Length, totalSize, _storagePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get storage info: {e.Message}");
                return (0, 0, _storagePath);
            }
        }
    }

    
    /// Helper class for JSON serialization of dictionaries
  
    [Serializable]
    public class SerializableDictionary
    {
        public List<string> keys = new List<string>();
        public List<string> values = new List<string>();

        public SerializableDictionary() { }

        public SerializableDictionary(Dictionary<string, object> dict)
        {
            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value?.ToString() ?? "");
            }
        }

        public Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                dict[keys[i]] = values[i];
            }
            return dict;
        }
    }
}