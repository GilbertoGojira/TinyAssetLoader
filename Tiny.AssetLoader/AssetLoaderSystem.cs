using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Tiny;
using Unity.Tiny.GenericAssetLoading;

namespace Tiny.AssetLoader {

  public static class AssetLoaderNativeCalls {
#if !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("lib_tiny_assetloader", EntryPoint = "startAssetLoad", CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
    public static extern long StartAssetLoad([System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPStr)] string file);

    [System.Runtime.InteropServices.DllImport("lib_tiny_assetloader", EntryPoint = "abortAssetLoad")]
    public static extern void AbortAssetLoad(int index);

    [System.Runtime.InteropServices.DllImport("lib_tiny_assetloader", EntryPoint = "getAssetData")]
    public static extern void GetAssetData(int index, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.LPArray)] byte[] data);

    [System.Runtime.InteropServices.DllImport("lib_tiny_assetloader", EntryPoint = "getAssetSize")]
    public static extern int GetAssetSize(int index);

    [System.Runtime.InteropServices.DllImport("lib_tiny_assetloader", EntryPoint = "getAssetStatusText")]
    public static extern string GetAssetStatusText(int index);

    [System.Runtime.InteropServices.DllImport("lib_tiny_assetloader", EntryPoint = "freeAsset")]
    public static extern void FreeAsset(int index);

    [System.Runtime.InteropServices.DllImport("lib_tiny_assetloader", EntryPoint = "checkAssetLoad")]
    public static extern int CheckAssetLoad(int index, ref int handle);
#else

    struct EditorData {
      public int Status;
      public byte[] Data;
    }

    static System.Collections.Generic.List<EditorData> _loadedData = 
      new System.Collections.Generic.List<EditorData>();

    public static long StartAssetLoad(string file) {
      var data = System.IO.File.Exists(file) ? System.IO.File.ReadAllBytes(file) : null;
      var loadedData = new EditorData {
        Data = data,
        Status = data != null ? 3 : 2
      };
      var index = _loadedData.FindIndex(d => d.Data == null);
      if (index == -1) {
        _loadedData.Add(loadedData);
        index = _loadedData.Count - 1;
      } else
        _loadedData[index] = loadedData;
      return _loadedData.Count - 1;
    }

    public static void AbortAssetLoad(int index) { }

    public static void GetAssetData(int index, byte[] data) {
      Array.Copy(_loadedData[index].Data, data, _loadedData[index].Data.Length);
    }

    public static int GetAssetSize(int index) {
      return _loadedData[index].Data?.Length ?? 0;
    }

    public static string GetAssetStatusText(int index) {
      return string.Empty;
    }

    public static void FreeAsset(int index) {
      _loadedData[index] = default;
    }

    public static int CheckAssetLoad(int index, ref int handle) {
      handle = index;
      return index >= 0 && index < _loadedData.Count ? _loadedData[index].Status : 2;
    }
#endif
  }  

  public unsafe class AssetLoader : IGenericAssetLoader<AssetState, AssetData, AssetLoadFromFile, AssetLoading> {
    public LoadResult CheckLoading(IntPtr cppwrapper, EntityManager man, Entity e, ref AssetState thing, ref AssetData native, ref AssetLoadFromFile source, ref AssetLoading loading) {
      int newHandle = 0;
      var res = AssetLoaderNativeCalls.CheckAssetLoad((int)loading.LoadingId, ref newHandle);      
      if (res == 0)
        return LoadResult.stillWorking;
      native.Handle = newHandle;
      var statusText = AssetLoaderNativeCalls.GetAssetStatusText(native.Handle);
      if (res == 2) {
        thing.Status = AssetStatus.LoadError;
        FreeNative(man, e, ref native);
        Debug.LogWarning($"Failed to load asset '{man.GetBufferAsString<AssetFilename>(e)}' with error '{statusText}' and handle {native.Handle}");
        return LoadResult.failed;
      }
      thing.Status = AssetStatus.Loaded;
      var data = new byte[AssetLoaderNativeCalls.GetAssetSize(native.Handle)];
      AssetLoaderNativeCalls.GetAssetData(native.Handle, data);
      man.AddBufferFromByteArray<AssetBuffer>(e, data);
      Debug.Log($"Load asset '{man.GetBufferAsString<AssetFilename>(e)}' of size {data.Length} and status '{statusText}' and handle {native.Handle}");
      return LoadResult.success;
    }

    public void FinishLoading(EntityManager man, Entity e, ref AssetState thing, ref AssetData native, ref AssetLoading loading) {
      FreeNative(man, e, ref native);
    }

    public void FreeNative(EntityManager man, Entity e, ref AssetData native) {
      if (native.Handle >= 0)
        AssetLoaderNativeCalls.FreeAsset(native.Handle);
    }

    public void StartLoad(EntityManager man, Entity e, ref AssetState thing, ref AssetData native, ref AssetLoadFromFile source, ref AssetLoading loading) {
      if (loading.LoadingId != 0)
        AssetLoaderNativeCalls.AbortAssetLoad((int)loading.LoadingId);
      var assetFile = man.GetBufferAsString<AssetFilename>(e);
      thing.Status = AssetStatus.Loading;
      loading.LoadingId = AssetLoaderNativeCalls.StartAssetLoad(assetFile);
    }
  }

  [UpdateInGroup(typeof(InitializationSystemGroup))]
  public class AssetLoaderSystem : GenericAssetLoader<AssetState, AssetData, AssetLoadFromFile, AssetLoading> {
    protected override void OnCreate() {
      base.OnCreate();
      c = new AssetLoader();
    }

    protected override void OnUpdate() {
      // Fills the required components to proceed to the next step of loading
      var ecb = new EntityCommandBuffer(Allocator.Temp);
      Entities
        .WithNone<AssetState,AssetLoadFromFile>()
        .WithAll<AssetFilename>()
        .ForEach((Entity entity) => {
          ecb.AddComponent<AssetState>(entity);
          ecb.AddComponent<AssetLoadFromFile>(entity);
        }).Run();
      ecb.Playback(EntityManager);
      ecb.Dispose();
      // loading
      base.OnUpdate();
    }
  }

}
