// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: cakelab/arflow_grpc/v1/gyroscope_frame.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace CakeLab.ARFlow.Grpc.V1 {

  /// <summary>Holder for reflection information generated from cakelab/arflow_grpc/v1/gyroscope_frame.proto</summary>
  public static partial class GyroscopeFrameReflection {

    #region Descriptor
    /// <summary>File descriptor for cakelab/arflow_grpc/v1/gyroscope_frame.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static GyroscopeFrameReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CixjYWtlbGFiL2FyZmxvd19ncnBjL3YxL2d5cm9zY29wZV9mcmFtZS5wcm90",
            "bxIWY2FrZWxhYi5hcmZsb3dfZ3JwYy52MRonY2FrZWxhYi9hcmZsb3dfZ3Jw",
            "Yy92MS9xdWF0ZXJuaW9uLnByb3RvGiRjYWtlbGFiL2FyZmxvd19ncnBjL3Yx",
            "L3ZlY3RvcjMucHJvdG8aH2dvb2dsZS9wcm90b2J1Zi90aW1lc3RhbXAucHJv",
            "dG8i3QIKDkd5cm9zY29wZUZyYW1lEkUKEGRldmljZV90aW1lc3RhbXAYASAB",
            "KAsyGi5nb29nbGUucHJvdG9idWYuVGltZXN0YW1wUg9kZXZpY2VUaW1lc3Rh",
            "bXASPgoIYXR0aXR1ZGUYAiABKAsyIi5jYWtlbGFiLmFyZmxvd19ncnBjLnYx",
            "LlF1YXRlcm5pb25SCGF0dGl0dWRlEkQKDXJvdGF0aW9uX3JhdGUYAyABKAsy",
            "Hy5jYWtlbGFiLmFyZmxvd19ncnBjLnYxLlZlY3RvcjNSDHJvdGF0aW9uUmF0",
            "ZRI5CgdncmF2aXR5GAQgASgLMh8uY2FrZWxhYi5hcmZsb3dfZ3JwYy52MS5W",
            "ZWN0b3IzUgdncmF2aXR5EkMKDGFjY2VsZXJhdGlvbhgFIAEoCzIfLmNha2Vs",
            "YWIuYXJmbG93X2dycGMudjEuVmVjdG9yM1IMYWNjZWxlcmF0aW9uQqgBChpj",
            "b20uY2FrZWxhYi5hcmZsb3dfZ3JwYy52MUITR3lyb3Njb3BlRnJhbWVQcm90",
            "b1ABogIDQ0FYqgIWQ2FrZUxhYi5BUkZsb3cuR3JwYy5WMcoCFUNha2VsYWJc",
            "QXJmbG93R3JwY1xWMeICIUNha2VsYWJcQXJmbG93R3JwY1xWMVxHUEJNZXRh",
            "ZGF0YeoCF0Nha2VsYWI6OkFyZmxvd0dycGM6OlYxYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::CakeLab.ARFlow.Grpc.V1.QuaternionReflection.Descriptor, global::CakeLab.ARFlow.Grpc.V1.Vector3Reflection.Descriptor, global::Google.Protobuf.WellKnownTypes.TimestampReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::CakeLab.ARFlow.Grpc.V1.GyroscopeFrame), global::CakeLab.ARFlow.Grpc.V1.GyroscopeFrame.Parser, new[]{ "DeviceTimestamp", "Attitude", "RotationRate", "Gravity", "Acceleration" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  [global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
  public sealed partial class GyroscopeFrame : pb::IMessage<GyroscopeFrame>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<GyroscopeFrame> _parser = new pb::MessageParser<GyroscopeFrame>(() => new GyroscopeFrame());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<GyroscopeFrame> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::CakeLab.ARFlow.Grpc.V1.GyroscopeFrameReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public GyroscopeFrame() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public GyroscopeFrame(GyroscopeFrame other) : this() {
      deviceTimestamp_ = other.deviceTimestamp_ != null ? other.deviceTimestamp_.Clone() : null;
      attitude_ = other.attitude_ != null ? other.attitude_.Clone() : null;
      rotationRate_ = other.rotationRate_ != null ? other.rotationRate_.Clone() : null;
      gravity_ = other.gravity_ != null ? other.gravity_.Clone() : null;
      acceleration_ = other.acceleration_ != null ? other.acceleration_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public GyroscopeFrame Clone() {
      return new GyroscopeFrame(this);
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

    /// <summary>Field number for the "attitude" field.</summary>
    public const int AttitudeFieldNumber = 2;
    private global::CakeLab.ARFlow.Grpc.V1.Quaternion attitude_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Quaternion Attitude {
      get { return attitude_; }
      set {
        attitude_ = value;
      }
    }

    /// <summary>Field number for the "rotation_rate" field.</summary>
    public const int RotationRateFieldNumber = 3;
    private global::CakeLab.ARFlow.Grpc.V1.Vector3 rotationRate_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Vector3 RotationRate {
      get { return rotationRate_; }
      set {
        rotationRate_ = value;
      }
    }

    /// <summary>Field number for the "gravity" field.</summary>
    public const int GravityFieldNumber = 4;
    private global::CakeLab.ARFlow.Grpc.V1.Vector3 gravity_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Vector3 Gravity {
      get { return gravity_; }
      set {
        gravity_ = value;
      }
    }

    /// <summary>Field number for the "acceleration" field.</summary>
    public const int AccelerationFieldNumber = 5;
    private global::CakeLab.ARFlow.Grpc.V1.Vector3 acceleration_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Vector3 Acceleration {
      get { return acceleration_; }
      set {
        acceleration_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as GyroscopeFrame);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(GyroscopeFrame other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(DeviceTimestamp, other.DeviceTimestamp)) return false;
      if (!object.Equals(Attitude, other.Attitude)) return false;
      if (!object.Equals(RotationRate, other.RotationRate)) return false;
      if (!object.Equals(Gravity, other.Gravity)) return false;
      if (!object.Equals(Acceleration, other.Acceleration)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (deviceTimestamp_ != null) hash ^= DeviceTimestamp.GetHashCode();
      if (attitude_ != null) hash ^= Attitude.GetHashCode();
      if (rotationRate_ != null) hash ^= RotationRate.GetHashCode();
      if (gravity_ != null) hash ^= Gravity.GetHashCode();
      if (acceleration_ != null) hash ^= Acceleration.GetHashCode();
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
      if (attitude_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Attitude);
      }
      if (rotationRate_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(RotationRate);
      }
      if (gravity_ != null) {
        output.WriteRawTag(34);
        output.WriteMessage(Gravity);
      }
      if (acceleration_ != null) {
        output.WriteRawTag(42);
        output.WriteMessage(Acceleration);
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
      if (attitude_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Attitude);
      }
      if (rotationRate_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(RotationRate);
      }
      if (gravity_ != null) {
        output.WriteRawTag(34);
        output.WriteMessage(Gravity);
      }
      if (acceleration_ != null) {
        output.WriteRawTag(42);
        output.WriteMessage(Acceleration);
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
      if (attitude_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Attitude);
      }
      if (rotationRate_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(RotationRate);
      }
      if (gravity_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Gravity);
      }
      if (acceleration_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Acceleration);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(GyroscopeFrame other) {
      if (other == null) {
        return;
      }
      if (other.deviceTimestamp_ != null) {
        if (deviceTimestamp_ == null) {
          DeviceTimestamp = new global::Google.Protobuf.WellKnownTypes.Timestamp();
        }
        DeviceTimestamp.MergeFrom(other.DeviceTimestamp);
      }
      if (other.attitude_ != null) {
        if (attitude_ == null) {
          Attitude = new global::CakeLab.ARFlow.Grpc.V1.Quaternion();
        }
        Attitude.MergeFrom(other.Attitude);
      }
      if (other.rotationRate_ != null) {
        if (rotationRate_ == null) {
          RotationRate = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
        }
        RotationRate.MergeFrom(other.RotationRate);
      }
      if (other.gravity_ != null) {
        if (gravity_ == null) {
          Gravity = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
        }
        Gravity.MergeFrom(other.Gravity);
      }
      if (other.acceleration_ != null) {
        if (acceleration_ == null) {
          Acceleration = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
        }
        Acceleration.MergeFrom(other.Acceleration);
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
          case 18: {
            if (attitude_ == null) {
              Attitude = new global::CakeLab.ARFlow.Grpc.V1.Quaternion();
            }
            input.ReadMessage(Attitude);
            break;
          }
          case 26: {
            if (rotationRate_ == null) {
              RotationRate = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(RotationRate);
            break;
          }
          case 34: {
            if (gravity_ == null) {
              Gravity = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Gravity);
            break;
          }
          case 42: {
            if (acceleration_ == null) {
              Acceleration = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Acceleration);
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
          case 18: {
            if (attitude_ == null) {
              Attitude = new global::CakeLab.ARFlow.Grpc.V1.Quaternion();
            }
            input.ReadMessage(Attitude);
            break;
          }
          case 26: {
            if (rotationRate_ == null) {
              RotationRate = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(RotationRate);
            break;
          }
          case 34: {
            if (gravity_ == null) {
              Gravity = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Gravity);
            break;
          }
          case 42: {
            if (acceleration_ == null) {
              Acceleration = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Acceleration);
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