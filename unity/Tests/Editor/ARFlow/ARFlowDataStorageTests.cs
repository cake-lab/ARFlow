using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using ARFlow;

public class ARFlowDataStorageTests
{
    private ARFlowDataStorage _storage;

    [SetUp]
    public void SetUp()
    {
        _storage = new ARFlowDataStorage(5);
        _storage.ClearStorage();
    }

    [TearDown]
    public void TearDown()
    {
        _storage.ClearStorage();
    }

    private DataFrameRequest CreateDummyFrame(string uid = "test")
    {
        var frame = new DataFrameRequest
        {
            Uid = uid,
            Color = Google.Protobuf.ByteString.CopyFrom(new byte[] { 1, 2, 3 }),
            Depth = Google.Protobuf.ByteString.CopyFrom(new byte[] { 4, 5, 6 }),
            Transform = Google.Protobuf.ByteString.CopyFrom(new byte[] { 7, 8, 9 })
        };
        return frame;
    }

    [Test]
    public async Task StoreAndLoadFrameAsync_WritesAndReadsCorrectly()
    {
        var metadata = new Dictionary<string, object> { { "foo", "bar" }, { "num", 42 } };
        var frame = CreateDummyFrame();
        var filePath = await _storage.StoreFrameAsync(frame, metadata);
        Assert.IsTrue(File.Exists(filePath));

        var (loadedFrame, loadedMetadata) = await _storage.LoadFrameAsync(filePath);
        Assert.IsNotNull(loadedFrame);
        Assert.AreEqual(frame.Uid, loadedFrame.Uid);
        Assert.AreEqual("bar", loadedMetadata["foo"]);
        Assert.AreEqual("42", loadedMetadata["num"]);
    }

    [Test]
    public void StoreFrame_Synchronous_Works()
    {
        var frame = CreateDummyFrame("sync");
        var filePath = _storage.StoreFrame(frame);
        Assert.IsTrue(File.Exists(filePath));
    }

    [Test]
    public void GetStoredFrames_ReturnsCorrectFiles()
    {
        var frame1 = CreateDummyFrame("a");
        var frame2 = CreateDummyFrame("b");
        _storage.StoreFrame(frame1);
        _storage.StoreFrame(frame2);
        var files = _storage.GetStoredFrames();
        Assert.AreEqual(2, files.Count);
    }

    [Test]
    public void ClearStorage_RemovesAllFiles()
    {
        var frame = CreateDummyFrame();
        _storage.StoreFrame(frame);
        _storage.ClearStorage();
        var files = _storage.GetStoredFrames();
        Assert.AreEqual(0, files.Count);
    }

    [Test]
    public void GetStorageInfo_ReturnsCorrectStats()
    {
        var frame = CreateDummyFrame();
        _storage.StoreFrame(frame);
        var (count, size, path) = _storage.GetStorageInfo();
        Assert.AreEqual(1, count);
        Assert.IsTrue(size > 0);
        Assert.IsTrue(Directory.Exists(path));
    }

    [Test]
    public void StorageLimit_DeletesOldest()
    {
        for (int i = 0; i < 7; i++)
        {
            _storage.StoreFrame(CreateDummyFrame(i.ToString()));
        }
        var files = _storage.GetStoredFrames();
        Assert.AreEqual(5, files.Count);
    }
}
