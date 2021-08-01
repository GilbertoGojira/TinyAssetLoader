using Unity.Entities;

namespace Tiny.AssetLoader {

  public enum AssetStatus {
    Invalid,
    Loaded,
    Loading,
    LoadError
  }

  public struct AssetState : IComponentData {
    public AssetStatus Status;
  }

  public struct AssetData : ISystemStateComponentData {
    public int Handle;
  }

  public struct AssetLoadFromFile : IComponentData { }

  public struct AssetLoading : ISystemStateComponentData {
    public long LoadingId;
  }

  public struct AssetFilename : IBufferElementData {
    public char c;
  }

  public struct AssetBuffer : IBufferElementData {
    public byte Value;
  }
}