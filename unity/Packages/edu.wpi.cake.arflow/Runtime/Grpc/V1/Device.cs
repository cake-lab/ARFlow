// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: cakelab/arflow_grpc/v1/device.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021, 8981
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace CakeLab.ARFlow.Grpc.V1 {

  /// <summary>Holder for reflection information generated from cakelab/arflow_grpc/v1/device.proto</summary>
  public static partial class DeviceReflection {

    #region Descriptor
    /// <summary>File descriptor for cakelab/arflow_grpc/v1/device.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static DeviceReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CiNjYWtlbGFiL2FyZmxvd19ncnBjL3YxL2RldmljZS5wcm90bxIWY2FrZWxh",
            "Yi5hcmZsb3dfZ3JwYy52MSLSAQoGRGV2aWNlEhQKBW1vZGVsGAEgASgJUgVt",
            "b2RlbBISCgRuYW1lGAIgASgJUgRuYW1lEjcKBHR5cGUYAyABKA4yIy5jYWtl",
            "bGFiLmFyZmxvd19ncnBjLnYxLkRldmljZS5UeXBlUgR0eXBlEhAKA3VpZBgE",
            "IAEoCVIDdWlkIlMKBFR5cGUSFAoQVFlQRV9VTlNQRUNJRklFRBAAEhEKDVRZ",
            "UEVfSEFOREhFTEQQARIQCgxUWVBFX0NPTlNPTEUQAhIQCgxUWVBFX0RFU0tU",
            "T1AQA0KgAQoaY29tLmNha2VsYWIuYXJmbG93X2dycGMudjFCC0RldmljZVBy",
            "b3RvUAGiAgNDQViqAhZDYWtlTGFiLkFSRmxvdy5HcnBjLlYxygIVQ2FrZWxh",
            "YlxBcmZsb3dHcnBjXFYx4gIhQ2FrZWxhYlxBcmZsb3dHcnBjXFYxXEdQQk1l",
            "dGFkYXRh6gIXQ2FrZWxhYjo6QXJmbG93R3JwYzo6VjFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::CakeLab.ARFlow.Grpc.V1.Device), global::CakeLab.ARFlow.Grpc.V1.Device.Parser, new[]{ "Model", "Name", "Type", "Uid" }, null, new[]{ typeof(global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type) }, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  /// https://docs.unity3d.com/ScriptReference/SystemInfo.html
  /// </summary>
  [global::System.Diagnostics.DebuggerDisplayAttribute("{ToString(),nq}")]
  public sealed partial class Device : pb::IMessage<Device>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<Device> _parser = new pb::MessageParser<Device>(() => new Device());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pb::MessageParser<Device> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::CakeLab.ARFlow.Grpc.V1.DeviceReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Device() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Device(Device other) : this() {
      model_ = other.model_;
      name_ = other.name_;
      type_ = other.type_;
      uid_ = other.uid_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public Device Clone() {
      return new Device(this);
    }

    /// <summary>Field number for the "model" field.</summary>
    public const int ModelFieldNumber = 1;
    private string model_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Model {
      get { return model_; }
      set {
        model_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "name" field.</summary>
    public const int NameFieldNumber = 2;
    private string name_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Name {
      get { return name_; }
      set {
        name_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "type" field.</summary>
    public const int TypeFieldNumber = 3;
    private global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type type_ = global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type.Unspecified;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type Type {
      get { return type_; }
      set {
        type_ = value;
      }
    }

    /// <summary>Field number for the "uid" field.</summary>
    public const int UidFieldNumber = 4;
    private string uid_ = "";
    /// <summary>
    /// Unique identifier. Guanranteed to be unique across all devices.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public string Uid {
      get { return uid_; }
      set {
        uid_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override bool Equals(object other) {
      return Equals(other as Device);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public bool Equals(Device other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Model != other.Model) return false;
      if (Name != other.Name) return false;
      if (Type != other.Type) return false;
      if (Uid != other.Uid) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public override int GetHashCode() {
      int hash = 1;
      if (Model.Length != 0) hash ^= Model.GetHashCode();
      if (Name.Length != 0) hash ^= Name.GetHashCode();
      if (Type != global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type.Unspecified) hash ^= Type.GetHashCode();
      if (Uid.Length != 0) hash ^= Uid.GetHashCode();
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
      if (Model.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Model);
      }
      if (Name.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Name);
      }
      if (Type != global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type.Unspecified) {
        output.WriteRawTag(24);
        output.WriteEnum((int) Type);
      }
      if (Uid.Length != 0) {
        output.WriteRawTag(34);
        output.WriteString(Uid);
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
      if (Model.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Model);
      }
      if (Name.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(Name);
      }
      if (Type != global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type.Unspecified) {
        output.WriteRawTag(24);
        output.WriteEnum((int) Type);
      }
      if (Uid.Length != 0) {
        output.WriteRawTag(34);
        output.WriteString(Uid);
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
      if (Model.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Model);
      }
      if (Name.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Name);
      }
      if (Type != global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type.Unspecified) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) Type);
      }
      if (Uid.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Uid);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public void MergeFrom(Device other) {
      if (other == null) {
        return;
      }
      if (other.Model.Length != 0) {
        Model = other.Model;
      }
      if (other.Name.Length != 0) {
        Name = other.Name;
      }
      if (other.Type != global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type.Unspecified) {
        Type = other.Type;
      }
      if (other.Uid.Length != 0) {
        Uid = other.Uid;
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
            Model = input.ReadString();
            break;
          }
          case 18: {
            Name = input.ReadString();
            break;
          }
          case 24: {
            Type = (global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type) input.ReadEnum();
            break;
          }
          case 34: {
            Uid = input.ReadString();
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
            Model = input.ReadString();
            break;
          }
          case 18: {
            Name = input.ReadString();
            break;
          }
          case 24: {
            Type = (global::CakeLab.ARFlow.Grpc.V1.Device.Types.Type) input.ReadEnum();
            break;
          }
          case 34: {
            Uid = input.ReadString();
            break;
          }
        }
      }
    }
    #endif

    #region Nested types
    /// <summary>Container for nested types declared in the Device message type.</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("protoc", null)]
    public static partial class Types {
      public enum Type {
        [pbr::OriginalName("TYPE_UNSPECIFIED")] Unspecified = 0,
        [pbr::OriginalName("TYPE_HANDHELD")] Handheld = 1,
        [pbr::OriginalName("TYPE_CONSOLE")] Console = 2,
        [pbr::OriginalName("TYPE_DESKTOP")] Desktop = 3,
      }

    }
    #endregion

  }

  #endregion

}

#endregion Designer generated code