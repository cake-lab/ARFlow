// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: cakelab/arflow_grpc/v1/save_ar_frames_request.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace CakeLab.ARFlow.Grpc.V1 {

  /// <summary>Holder for reflection information generated from cakelab/arflow_grpc/v1/save_ar_frames_request.proto</summary>
  public static partial class SaveArFramesRequestReflection {

    #region Descriptor
    /// <summary>File descriptor for cakelab/arflow_grpc/v1/save_ar_frames_request.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static SaveArFramesRequestReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CjNjYWtlbGFiL2FyZmxvd19ncnBjL3YxL3NhdmVfYXJfZnJhbWVzX3JlcXVl",
            "c3QucHJvdG8SFmNha2VsYWIuYXJmbG93X2dycGMudjEaJWNha2VsYWIvYXJm",
            "bG93X2dycGMvdjEvYXJfZnJhbWUucHJvdG8aI2Nha2VsYWIvYXJmbG93X2dy",
            "cGMvdjEvZGV2aWNlLnByb3RvGiRjYWtlbGFiL2FyZmxvd19ncnBjL3YxL3Nl",
            "c3Npb24ucHJvdG8iygEKE1NhdmVBUkZyYW1lc1JlcXVlc3QSQgoKc2Vzc2lv",
            "bl9pZBgBIAEoCzIjLmNha2VsYWIuYXJmbG93X2dycGMudjEuU2Vzc2lvblV1",
            "aWRSCXNlc3Npb25JZBI2CgZkZXZpY2UYAiABKAsyHi5jYWtlbGFiLmFyZmxv",
            "d19ncnBjLnYxLkRldmljZVIGZGV2aWNlEjcKBmZyYW1lcxgDIAMoCzIfLmNh",
            "a2VsYWIuYXJmbG93X2dycGMudjEuQVJGcmFtZVIGZnJhbWVzQq0BChpjb20u",
            "Y2FrZWxhYi5hcmZsb3dfZ3JwYy52MUIYU2F2ZUFyRnJhbWVzUmVxdWVzdFBy",
            "b3RvUAGiAgNDQViqAhZDYWtlTGFiLkFSRmxvdy5HcnBjLlYxygIVQ2FrZWxh",
            "YlxBcmZsb3dHcnBjXFYx4gIhQ2FrZWxhYlxBcmZsb3dHcnBjXFYxXEdQQk1l",
            "dGFkYXRh6gIXQ2FrZWxhYjo6QXJmbG93R3JwYzo6VjFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::CakeLab.ARFlow.Grpc.V1.ArFrameReflection.Descriptor, global::CakeLab.ARFlow.Grpc.V1.DeviceReflection.Descriptor, global::CakeLab.ARFlow.Grpc.V1.SessionReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::CakeLab.ARFlow.Grpc.V1.SaveARFramesRequest), global::CakeLab.ARFlow.Grpc.V1.SaveARFramesRequest.Parser, new[]{ "SessionId", "Device", "Frames" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  [global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
  public sealed partial class SaveARFramesRequest : pb::IMessage<SaveARFramesRequest>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<SaveARFramesRequest> _parser = new pb::MessageParser<SaveARFramesRequest>(() => new SaveARFramesRequest());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<SaveARFramesRequest> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::CakeLab.ARFlow.Grpc.V1.SaveArFramesRequestReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public SaveARFramesRequest() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public SaveARFramesRequest(SaveARFramesRequest other) : this() {
      sessionId_ = other.sessionId_ != null ? other.sessionId_.Clone() : null;
      device_ = other.device_ != null ? other.device_.Clone() : null;
      frames_ = other.frames_.Clone();
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public SaveARFramesRequest Clone() {
      return new SaveARFramesRequest(this);
    }

    /// <summary>Field number for the "session_id" field.</summary>
    public const int SessionIdFieldNumber = 1;
    private global::CakeLab.ARFlow.Grpc.V1.SessionUuid sessionId_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.SessionUuid SessionId {
      get { return sessionId_; }
      set {
        sessionId_ = value;
      }
    }

    /// <summary>Field number for the "device" field.</summary>
    public const int DeviceFieldNumber = 2;
    private global::CakeLab.ARFlow.Grpc.V1.Device device_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Device Device {
      get { return device_; }
      set {
        device_ = value;
      }
    }

    /// <summary>Field number for the "frames" field.</summary>
    public const int FramesFieldNumber = 3;
    private static readonly pb::FieldCodec<global::CakeLab.ARFlow.Grpc.V1.ARFrame> _repeated_frames_codec
        = pb::FieldCodec.ForMessage(26, global::CakeLab.ARFlow.Grpc.V1.ARFrame.Parser);
    private readonly pbc::RepeatedField<global::CakeLab.ARFlow.Grpc.V1.ARFrame> frames_ = new pbc::RepeatedField<global::CakeLab.ARFlow.Grpc.V1.ARFrame>();
    /// <summary>
    ///*
    /// @exclude
    /// See https://github.com/protocolbuffers/protobuf/issues/2592
    /// to see why we cannot use oneof of repeated fields here. The
    /// workaround here is to use a repeated field of oneof types
    /// and determine the type of each element at runtime.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public pbc::RepeatedField<global::CakeLab.ARFlow.Grpc.V1.ARFrame> Frames {
      get { return frames_; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as SaveARFramesRequest);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(SaveARFramesRequest other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(SessionId, other.SessionId)) return false;
      if (!object.Equals(Device, other.Device)) return false;
      if(!frames_.Equals(other.frames_)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (sessionId_ != null) hash ^= SessionId.GetHashCode();
      if (device_ != null) hash ^= Device.GetHashCode();
      hash ^= frames_.GetHashCode();
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
      if (sessionId_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(SessionId);
      }
      if (device_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Device);
      }
      frames_.WriteTo(output, _repeated_frames_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (sessionId_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(SessionId);
      }
      if (device_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Device);
      }
      frames_.WriteTo(ref output, _repeated_frames_codec);
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public int CalculateSize() {
      int size = 0;
      if (sessionId_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(SessionId);
      }
      if (device_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Device);
      }
      size += frames_.CalculateSize(_repeated_frames_codec);
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(SaveARFramesRequest other) {
      if (other == null) {
        return;
      }
      if (other.sessionId_ != null) {
        if (sessionId_ == null) {
          SessionId = new global::CakeLab.ARFlow.Grpc.V1.SessionUuid();
        }
        SessionId.MergeFrom(other.SessionId);
      }
      if (other.device_ != null) {
        if (device_ == null) {
          Device = new global::CakeLab.ARFlow.Grpc.V1.Device();
        }
        Device.MergeFrom(other.Device);
      }
      frames_.Add(other.frames_);
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
            if (sessionId_ == null) {
              SessionId = new global::CakeLab.ARFlow.Grpc.V1.SessionUuid();
            }
            input.ReadMessage(SessionId);
            break;
          }
          case 18: {
            if (device_ == null) {
              Device = new global::CakeLab.ARFlow.Grpc.V1.Device();
            }
            input.ReadMessage(Device);
            break;
          }
          case 26: {
            frames_.AddEntriesFrom(input, _repeated_frames_codec);
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
            if (sessionId_ == null) {
              SessionId = new global::CakeLab.ARFlow.Grpc.V1.SessionUuid();
            }
            input.ReadMessage(SessionId);
            break;
          }
          case 18: {
            if (device_ == null) {
              Device = new global::CakeLab.ARFlow.Grpc.V1.Device();
            }
            input.ReadMessage(Device);
            break;
          }
          case 26: {
            frames_.AddEntriesFrom(ref input, _repeated_frames_codec);
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
