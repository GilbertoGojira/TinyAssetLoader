using NUnit.Framework;
using System.IO;
using Unity.Tiny;

namespace Tiny.AssetLoader.Tests {
  public class AssetLoadTest : WorldBaseTest {

    [Test]
    public void AssetLoadSuccessTest() {
      var assetPath = Path.GetFullPath("Packages/com.gilcat.assetloader/Tiny.AssetLoader.Tests/files~/test.bin");
      var originalFileData = File.ReadAllBytes(assetPath);

      var entity = _entityManager.CreateEntity();
      _entityManager.AddBufferFromString<AssetFilename>(entity, assetPath);

      var system = _world.GetOrCreateSystem<AssetLoaderSystem>();
      system.Update();
      AssetState assetState;
      while((assetState = _entityManager.GetComponentData<AssetState>(entity)).Status == AssetStatus.Loading)
        system.Update();
      Assert.AreEqual(assetState.Status, AssetStatus.Loaded);
      var loadedFileData = _entityManager.GetBufferAsByteArray<AssetBuffer>(entity);
      Assert.AreEqual(originalFileData, loadedFileData);
    }

    [Test]
    public void AssetLoadFailTest() {
      var entity = _entityManager.CreateEntity(typeof(AssetState), typeof(AssetLoadFromFile));
      _entityManager.AddBuffer<AssetFilename>(entity);
      _entityManager.SetBufferFromString<AssetFilename>(entity, "BadFile.bin");

      var system = _world.GetOrCreateSystem<AssetLoaderSystem>();
      system.Update();
      AssetState assetState;
      while ((assetState = _entityManager.GetComponentData<AssetState>(entity)).Status == AssetStatus.Loading)
        system.Update();
      Assert.AreEqual(assetState.Status, AssetStatus.LoadError);
    }
  }
}
