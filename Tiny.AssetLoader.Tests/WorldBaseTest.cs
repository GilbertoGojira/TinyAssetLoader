using NUnit.Framework;
using Unity.Entities;

namespace Tiny.AssetLoader.Tests {
  public abstract class WorldBaseTest {

    protected World _world;
    protected EntityManager _entityManager;

    [SetUp]
    public void Initialize() {
      _world = World.DefaultGameObjectInjectionWorld = new World("Test World");
      _entityManager = _world.EntityManager;
    }
  }
}