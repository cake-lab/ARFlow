// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: cakelab/arflow_grpc/v1/pose.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace CakeLab.ARFlow.Grpc.V1 {

  /// <summary>Holder for reflection information generated from cakelab/arflow_grpc/v1/pose.proto</summary>
  public static partial class PoseReflection {

    #region Descriptor
    /// <summary>File descriptor for cakelab/arflow_grpc/v1/pose.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static PoseReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CiFjYWtlbGFiL2FyZmxvd19ncnBjL3YxL3Bvc2UucHJvdG8SFmNha2VsYWIu",
            "YXJmbG93X2dycGMudjEaJ2Nha2VsYWIvYXJmbG93X2dycGMvdjEvcXVhdGVy",
            "bmlvbi5wcm90bxokY2FrZWxhYi9hcmZsb3dfZ3JwYy92MS92ZWN0b3IzLnBy",
            "b3RvIqYCCgRQb3NlEjkKB2ZvcndhcmQYASABKAsyHy5jYWtlbGFiLmFyZmxv",
            "d19ncnBjLnYxLlZlY3RvcjNSB2ZvcndhcmQSOwoIcG9zaXRpb24YAiABKAsy",
            "Hy5jYWtlbGFiLmFyZmxvd19ncnBjLnYxLlZlY3RvcjNSCHBvc2l0aW9uEjUK",
            "BXJpZ2h0GAMgASgLMh8uY2FrZWxhYi5hcmZsb3dfZ3JwYy52MS5WZWN0b3Iz",
            "UgVyaWdodBI+Cghyb3RhdGlvbhgEIAEoCzIiLmNha2VsYWIuYXJmbG93X2dy",
            "cGMudjEuUXVhdGVybmlvblIIcm90YXRpb24SLwoCdXAYBSABKAsyHy5jYWtl",
            "bGFiLmFyZmxvd19ncnBjLnYxLlZlY3RvcjNSAnVwQp4BChpjb20uY2FrZWxh",
            "Yi5hcmZsb3dfZ3JwYy52MUIJUG9zZVByb3RvUAGiAgNDQViqAhZDYWtlTGFi",
            "LkFSRmxvdy5HcnBjLlYxygIVQ2FrZWxhYlxBcmZsb3dHcnBjXFYx4gIhQ2Fr",
            "ZWxhYlxBcmZsb3dHcnBjXFYxXEdQQk1ldGFkYXRh6gIXQ2FrZWxhYjo6QXJm",
            "bG93R3JwYzo6VjFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::CakeLab.ARFlow.Grpc.V1.QuaternionReflection.Descriptor, global::CakeLab.ARFlow.Grpc.V1.Vector3Reflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::CakeLab.ARFlow.Grpc.V1.Pose), global::CakeLab.ARFlow.Grpc.V1.Pose.Parser, new[]{ "Forward", "Position", "Right", "Rotation", "Up" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  ///&#x2F; https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Pose.html
  /// </summary>
  [global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
  public sealed partial class Pose : pb::IMessage<Pose>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<Pose> _parser = new pb::MessageParser<Pose>(() => new Pose());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<Pose> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::CakeLab.ARFlow.Grpc.V1.PoseReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Pose() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Pose(Pose other) : this() {
      forward_ = other.forward_ != null ? other.forward_.Clone() : null;
      position_ = other.position_ != null ? other.position_.Clone() : null;
      right_ = other.right_ != null ? other.right_.Clone() : null;
      rotation_ = other.rotation_ != null ? other.rotation_.Clone() : null;
      up_ = other.up_ != null ? other.up_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Pose Clone() {
      return new Pose(this);
    }

    /// <summary>Field number for the "forward" field.</summary>
    public const int ForwardFieldNumber = 1;
    private global::CakeLab.ARFlow.Grpc.V1.Vector3 forward_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Vector3 Forward {
      get { return forward_; }
      set {
        forward_ = value;
      }
    }

    /// <summary>Field number for the "position" field.</summary>
    public const int PositionFieldNumber = 2;
    private global::CakeLab.ARFlow.Grpc.V1.Vector3 position_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Vector3 Position {
      get { return position_; }
      set {
        position_ = value;
      }
    }

    /// <summary>Field number for the "right" field.</summary>
    public const int RightFieldNumber = 3;
    private global::CakeLab.ARFlow.Grpc.V1.Vector3 right_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Vector3 Right {
      get { return right_; }
      set {
        right_ = value;
      }
    }

    /// <summary>Field number for the "rotation" field.</summary>
    public const int RotationFieldNumber = 4;
    private global::CakeLab.ARFlow.Grpc.V1.Quaternion rotation_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Quaternion Rotation {
      get { return rotation_; }
      set {
        rotation_ = value;
      }
    }

    /// <summary>Field number for the "up" field.</summary>
    public const int UpFieldNumber = 5;
    private global::CakeLab.ARFlow.Grpc.V1.Vector3 up_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Vector3 Up {
      get { return up_; }
      set {
        up_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as Pose);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(Pose other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(Forward, other.Forward)) return false;
      if (!object.Equals(Position, other.Position)) return false;
      if (!object.Equals(Right, other.Right)) return false;
      if (!object.Equals(Rotation, other.Rotation)) return false;
      if (!object.Equals(Up, other.Up)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (forward_ != null) hash ^= Forward.GetHashCode();
      if (position_ != null) hash ^= Position.GetHashCode();
      if (right_ != null) hash ^= Right.GetHashCode();
      if (rotation_ != null) hash ^= Rotation.GetHashCode();
      if (up_ != null) hash ^= Up.GetHashCode();
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
      if (forward_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Forward);
      }
      if (position_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Position);
      }
      if (right_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(Right);
      }
      if (rotation_ != null) {
        output.WriteRawTag(34);
        output.WriteMessage(Rotation);
      }
      if (up_ != null) {
        output.WriteRawTag(42);
        output.WriteMessage(Up);
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
      if (forward_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(Forward);
      }
      if (position_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Position);
      }
      if (right_ != null) {
        output.WriteRawTag(26);
        output.WriteMessage(Right);
      }
      if (rotation_ != null) {
        output.WriteRawTag(34);
        output.WriteMessage(Rotation);
      }
      if (up_ != null) {
        output.WriteRawTag(42);
        output.WriteMessage(Up);
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
      if (forward_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Forward);
      }
      if (position_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Position);
      }
      if (right_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Right);
      }
      if (rotation_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Rotation);
      }
      if (up_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Up);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(Pose other) {
      if (other == null) {
        return;
      }
      if (other.forward_ != null) {
        if (forward_ == null) {
          Forward = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
        }
        Forward.MergeFrom(other.Forward);
      }
      if (other.position_ != null) {
        if (position_ == null) {
          Position = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
        }
        Position.MergeFrom(other.Position);
      }
      if (other.right_ != null) {
        if (right_ == null) {
          Right = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
        }
        Right.MergeFrom(other.Right);
      }
      if (other.rotation_ != null) {
        if (rotation_ == null) {
          Rotation = new global::CakeLab.ARFlow.Grpc.V1.Quaternion();
        }
        Rotation.MergeFrom(other.Rotation);
      }
      if (other.up_ != null) {
        if (up_ == null) {
          Up = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
        }
        Up.MergeFrom(other.Up);
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
            if (forward_ == null) {
              Forward = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Forward);
            break;
          }
          case 18: {
            if (position_ == null) {
              Position = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Position);
            break;
          }
          case 26: {
            if (right_ == null) {
              Right = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Right);
            break;
          }
          case 34: {
            if (rotation_ == null) {
              Rotation = new global::CakeLab.ARFlow.Grpc.V1.Quaternion();
            }
            input.ReadMessage(Rotation);
            break;
          }
          case 42: {
            if (up_ == null) {
              Up = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Up);
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
            if (forward_ == null) {
              Forward = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Forward);
            break;
          }
          case 18: {
            if (position_ == null) {
              Position = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Position);
            break;
          }
          case 26: {
            if (right_ == null) {
              Right = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Right);
            break;
          }
          case 34: {
            if (rotation_ == null) {
              Rotation = new global::CakeLab.ARFlow.Grpc.V1.Quaternion();
            }
            input.ReadMessage(Rotation);
            break;
          }
          case 42: {
            if (up_ == null) {
              Up = new global::CakeLab.ARFlow.Grpc.V1.Vector3();
            }
            input.ReadMessage(Up);
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
