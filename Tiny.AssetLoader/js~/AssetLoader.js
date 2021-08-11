mergeInto(LibraryManager.library, {
  startAssetLoad: function (filename) {
    filename = filename ? UTF8ToString(filename) : null;
    ut = ut || {};
    ut._HTML = ut._HTML || {};
    ut._HTML.assets = ut._HTML.assets || [];

    // local helper functions
    ut._HTML.addRequest = function (request) {
      var asset = {
        request: request,
        data: null,
        error: false
      };
      var i = 0;
      for (i = 0; i < ut._HTML.assets.length; ++i)
        if (!ut._HTML.assets[i]) {
          ut._HTML.assets[i] = asset;
          return i;
        }
      ut._HTML.assets.push(asset);
      return ut._HTML.assets.length - 1;
    };
    let request = new XMLHttpRequest();
    let index = ut._HTML.addRequest(request);
    request.responseType = "arraybuffer";
    request.open("GET", filename);
    request.onload = function () {
      ut._HTML.assets[index].data = new Uint8Array(request.response);
    };
    request.send();

    return index;
  },
  abortAssetLoad: function (index) {
    ut._HTML.assets[index].request.abort();
  },
  getAssetData: function (index, ptr) {
    HEAPU8.set(ut._HTML.assets[index].data, ptr);
  },
  getAssetSize: function (index) {
    return ut._HTML.assets[index].data.length;
  },
  getAssetStatusText: function (index) {
    var str = ut._HTML.assets[index].request.statusText;
    var bufferSize = lengthBytesUTF8(str) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(str, buffer, bufferSize);
    return buffer;
  },
  freeAsset: function (index) {
    ut._HTML.assets[index] = null;
  },
  checkAssetLoad: function (index, handle) {
    HEAP32[handle >> 2] = index;
    let request = ut._HTML.assets[index].request;
    let state = request.readyState;
    if (request.readyState > 0 && request.readyState < 4)
      state = 0;
    else if (request.status != 200)
      state = 2;
    return state;
  },
});