mergeInto(LibraryManager.library, {
  GameReady: function () {
    window.dispatchReactUnityEvent("GameReady");
  },
});