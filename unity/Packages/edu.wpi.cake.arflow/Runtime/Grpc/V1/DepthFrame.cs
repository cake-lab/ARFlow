// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: cakelab/arflow_grpc/v1/depth_frame.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace CakeLab.ARFlow.Grpc.V1 {

  /// <summary>Holder for reflection information generated from cakelab/arflow_grpc/v1/depth_frame.proto</summary>
  public static partial class DepthFrameReflection {

    #region Descriptor
    /// <summary>File descriptor for cakelab/arflow_grpc/v1/depth_frame.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static DepthFrameReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CihjYWtlbGFiL2FyZmxvd19ncnBjL3YxL2RlcHRoX2ZyYW1lLnByb3RvEhZj",
            "YWtlbGFiLmFyZmxvd19ncnBjLnYxGiljYWtlbGFiL2FyZmxvd19ncnBjL3Yx",
            "L3hyX2NwdV9pbWFnZS5wcm90bxofZ29vZ2xlL3Byb3RvYnVmL3RpbWVzdGFt",
            "cC5wcm90byLtAQoKRGVwdGhGcmFtZRJFChBkZXZpY2VfdGltZXN0YW1wGAEg",
            "ASgLMhouZ29vZ2xlLnByb3RvYnVmLlRpbWVzdGFtcFIPZGV2aWNlVGltZXN0",
            "YW1wEl4KLGVudmlyb25tZW50X2RlcHRoX3RlbXBvcmFsX3Ntb290aGluZ19l",
            "bmFibGVkGAIgASgIUihlbnZpcm9ubWVudERlcHRoVGVtcG9yYWxTbW9vdGhp",
            "bmdFbmFibGVkEjgKBWltYWdlGAMgASgLMiIuY2FrZWxhYi5hcmZsb3dfZ3Jw",
            "Yy52MS5YUkNwdUltYWdlUgVpbWFnZUKkAQoaY29tLmNha2VsYWIuYXJmbG93",
            "X2dycGMudjFCD0RlcHRoRnJhbWVQcm90b1ABogIDQ0FYqgIWQ2FrZUxhYi5B",
            "UkZsb3cuR3JwYy5WMcoCFUNha2VsYWJcQXJmbG93R3JwY1xWMeICIUNha2Vs",
            "YWJcQXJmbG93R3JwY1xWMVxHUEJNZXRhZGF0YeoCF0Nha2VsYWI6OkFyZmxv",
            "d0dycGM6OlYxYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::CakeLab.ARFlow.Grpc.V1.XrCpuImageReflection.Descriptor, global::Google.Protobuf.WellKnownTypes.TimestampReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::CakeLab.ARFlow.Grpc.V1.DepthFrame), global::CakeLab.ARFlow.Grpc.V1.DepthFrame.Parser, new[]{ "DeviceTimestamp", "EnvironmentDepthTemporalSmoothingEnabled", "Image" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  [global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
  public sealed partial class DepthFrame : pb::IMessage<DepthFrame>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<DepthFrame> _parser = new pb::MessageParser<DepthFrame>(() => new DepthFrame());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<DepthFrame> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::CakeLab.ARFlow.Grpc.V1.DepthFrameReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public DepthFrame() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public DepthFrame(DepthFrame other) : this() {
      deviceTimestamp_ = other.deviceTimestamp_ != null ? other.deviceTimestamp_.Clone() : null;
      environmentDepthTemporalSmoothingEnabled_ = other.environmentDepthTemporalSmoothingEnabled_;
      image_ = other.image_ != null ? other.image_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public DepthFrame Clone() {
      return new DepthFrame(this);
    }

    /// <summary>Field number for the "device_timestamp" field.</summary>
    public const int DeviceTimestampFieldNumber = 1;
    private global::Google.Protobuf.WellKnownTypes.Timestamp deviceTimestamp_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::Google.Protobuf.WellKnownTypes.Timestamp DeviceTimestamp {
      get { return deviceTimestamp_; }
      set {
        deviceTimestamp_ = value;
      }
    }

    /// <summary>Field number for the "environment_depth_temporal_smoothing_enabled" field.</summary>
    public const int EnvironmentDepthTemporalSmoothingEnabledFieldNumber = 2;
    private bool environmentDepthTemporalSmoothingEnabled_;
    /// <summary>
    ///&#x2F; https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/api/UnityEngine.XR.ARFoundation.AROcclusionManager.html#UnityEngine_XR_ARFoundation_AROcclusionManager_environmentDepthTemporalSmoothingEnabled
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool EnvironmentDepthTemporalSmoothingEnabled {
      get { return environmentDepthTemporalSmoothingEnabled_; }
      set {
        environmentDepthTemporalSmoothingEnabled_ = value;
      }
    }

    /// <summary>Field number for the "image" field.</summary>
    public const int ImageFieldNumber = 3;
    private global::CakeLab.ARFlow.Grpc.V1.XRCpuImage image_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.XRCpuImage Image {
      get { return image_; }
      set {
        image_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as DepthFrame);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(DepthFrame other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(DeviceTimestamp, other.DeviceTimestamp)) return false;
      if (EnvironmentDepthTemporalSmoothingEnabled != other.EnvironmentDepthTemporalSmoothingEnabled) return false;
      if (!object.Equals(Image, other.Image)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (deviceTimestamp_ != null) hash ^= DeviceTimestamp.GetHashCode();
      if (EnvironmentDepthTemporalSmoothingEnabled != false) hash ^= EnvironmentDepthTemporalSmoothingEnabled.GetHashCode();
      if (image_ != null) hash ^= Image.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (deviceTimestamp_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(DeviceTimestamp);
      }
      if (EnvironmentDepthTemporalSmoothingEnabled != false) {
        output.WriteRawTag(16);
        output.WriteBool(EnvironmentDepthTemporalSmoothingEnabled);
      }
      if (image_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(Image);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (deviceTimestamp_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(DeviceTimestamp);
      }
      if (EnvironmentDepthTemporalSmoothingEnabled != false) {
        output.WriteRawTag(16);
        output.WriteBool(EnvironmentDepthTemporalSmoothingEnabled);
      }
      if (image_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(Image);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (deviceTimestamp_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(DeviceTimestamp);
      }
      if (EnvironmentDepthTemporalSmoothingEnabled != false) {
        size += 1 + 1;
      }
      if (image_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Image);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(DepthFrame other) {
      if (other == null) {
        return;
      }
      if (other.deviceTimestamp_ != null) {
        if (deviceTimestamp_ == null) {
          DeviceTimestamp = new global::Google.Protobuf.WellKnownTypes.Timestamp();
        }
        DeviceTimestamp.MergeFrom(other.DeviceTimestamp);
      }
      if (other.EnvironmentDepthTemporalSmoothingEnabled != false) {
        EnvironmentDepthTemporalSmoothingEnabled = other.EnvironmentDepthTemporalSmoothingEnabled;
      }
      if (other.image_ != null) {
        if (image_ == null) {
          Image = new global::CakeLab.ARFlow.Grpc.V1.XRCpuImage();
        }
        Image.MergeFrom(other.Image);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
      if ((tag & 7) == 4) {
        // Abort on any end group tag.
        return;
      }
      switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (deviceTimestamp_ == null) {
              DeviceTimestamp = new global::Google.Protobuf.WellKnownTypes.Timestamp();
            }
            input.ReadMessage(DeviceTimestamp);
            break;
          }
          case 16: {
            EnvironmentDepthTemporalSmoothingEnabled = input.ReadBool();
            break;
          }
          case 26: {
            if (image_ == null) {
              Image = new global::CakeLab.ARFlow.Grpc.V1.XRCpuImage();
            }
            input.ReadMessage(Image);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
      if ((tag & 7) == 4) {
        // Abort on any end group tag.
        return;
      }
      switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            if (deviceTimestamp_ == null) {
              DeviceTimestamp = new global::Google.Protobuf.WellKnownTypes.Timestamp();
            }
            input.ReadMessage(DeviceTimestamp);
            break;
          }
          case 16: {
            EnvironmentDepthTemporalSmoothingEnabled = input.ReadBool();
            break;
          }
          case 26: {
            if (image_ == null) {
              Image = new global::CakeLab.ARFlow.Grpc.V1.XRCpuImage();
            }
            input.ReadMessage(Image);
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code
