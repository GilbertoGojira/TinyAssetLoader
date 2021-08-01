using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Tiny;

namespace Tiny.AssetLoader {
  public static class DynamicBufferExt {
    public static byte[] AsArray(this DynamicBuffer<byte> buffer) =>
      buffer.AsNativeArray().ToArray();

    public static unsafe void FromByteArray(this DynamicBuffer<byte> buffer, byte[] source) {
      if (source == null || source.Length == 0) {
        buffer.Clear();
        return;
      }

      fixed (byte* ptr = source) {
        var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(ptr, source.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
        buffer.CopyFrom(array);
      }
    }

    public static void AddBufferFromByteArray<T>(this EntityManager manager, Entity entity, byte[] value)
      where T : struct, IBufferElementData =>
      manager.AddBuffer<T>(entity)
        .Reinterpret<byte>()
        .FromByteArray(value);

    public static byte[] GetBufferAsByteArray<T>(this EntityManager manager, Entity entity)
      where T : struct, IBufferElementData {
      if (!manager.Exists(entity) || !manager.HasComponent<T>(entity))
        return new byte[0];
      return manager.GetBufferRO<T>(entity).Reinterpret<byte>().AsArray();
    }

    public static void AddBufferFromByteArray<T>(this EntityCommandBuffer manager, Entity entity, byte[] value)
      where T : struct, IBufferElementData =>
        manager.AddBuffer<T>(entity)
          .Reinterpret<byte>()
          .FromByteArray(value);

    public static void AddBufferFromString<T>(this EntityCommandBuffer manager, Entity entity, string value)
      where T : struct, IBufferElementData =>
        manager.AddBuffer<T>(entity)
          .Reinterpret<char>()
          .FromString(value);
  }
}
