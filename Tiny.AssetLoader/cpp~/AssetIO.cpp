#if !UNITY_WEBGL
#include <fstream>
#include <vector>
#include <Unity/Runtime.h>
#include "ThreadPool.h"

using namespace ut;
using namespace ut::ThreadPool;

struct Asset {
  uint8_t* data;
  size_t size;
  const char* error;
};

static std::vector<Asset> Assets(0);

static bool
LoadAssetFile(Asset& asset, const char* fname) {
  // binary mode is only for switching off newline translation
  std::ifstream file(fname, std::ios::binary);
  file.unsetf(std::ios::skipws);
  if (!file)
    return false;

  std::streampos file_size;
  file.seekg(0, std::ios::end);
  file_size = file.tellg();
  file.seekg(0, std::ios::beg);

  std::vector<unsigned char> vec;
  vec.reserve(file_size);
  vec.insert(vec.begin(),
    std::istream_iterator<unsigned char>(file),
    std::istream_iterator<unsigned char>());
  auto src = (char*) new char[vec.size()];
  std::copy(vec.begin(), vec.end(), src);
  asset.data = (uint8_t*)src;
  asset.size = file_size;
  file.close();
  return true;
}

class AsyncAssetLoader : public ThreadPool::Job {
public:
  // state needed for Do()
  std::string assetFile;
  Asset asset;

  virtual bool Do() {
    progress = 0;
    // simulate being slow
#if 0
    for (int i = 0; i < 20; i++) {
      std::this_thread::sleep_for(std::chrono::milliseconds(20));
      progress = i;
      if (abort)
        return false;
    }
#endif
    // actual work
    return LoadAssetFile(asset, assetFile.c_str());
  }
};

DOTS_EXPORT(int64_t)
startAssetLoad(const char* fname) {
  std::unique_ptr<AsyncAssetLoader> loader(new AsyncAssetLoader);
  loader->assetFile = fname;
  return Pool::GetInstance()->Enqueue(std::move(loader));
}

DOTS_EXPORT(int)
checkAssetLoad(int64_t loadId, int* handle) {
  *handle = -1;
  std::unique_ptr<ThreadPool::Job> resultTemp = Pool::GetInstance()->CheckAndRemove(loadId);
  if (!resultTemp)
    return 0; // still loading
  if (!resultTemp->GetReturnValue()) {
    resultTemp.reset(0);
    return 2; // failed
  }
  // put it into a local copy
  int found = -1;
  for (int i = 0; i < (int)Assets.size(); i++) {
    if (!Assets[i].data) {
      found = i;
      break;
    }
  }
  AsyncAssetLoader* result = (AsyncAssetLoader*)resultTemp.get();
  Asset asset = result->asset;
  if (found == -1) {
    Assets.push_back(asset);
    *handle = (int)Assets.size() - 1;
  }
  else {
    Assets[found] = asset;
    *handle = found;
  }
  return 1; // ok
}

DOTS_EXPORT(size_t)
getAssetSize(int handle) {
  return Assets[handle].size;
}

DOTS_EXPORT(void)
getAssetData(int handle, uint8_t* data) {
  for (size_t i = 0; i < Assets[handle].size; ++i)
    data[i] = Assets[handle].data[i];
}

DOTS_EXPORT(void)
abortAssetLoad(int handle) { }

DOTS_EXPORT(const char*)
getAssetStatusText(int handle) { return NULL; }

DOTS_EXPORT(void)
freeAsset(int handle) {
  if (handle < 0 || handle >= (int)Assets.size())
    return;
  delete Assets[handle].data;
  Assets[handle].data = 0;
}
#endif