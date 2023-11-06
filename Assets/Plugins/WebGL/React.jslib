mergeInto(LibraryManager.library, {
  GameReady: function (userName, score) {
    window.dispatchReactUnityEvent("GameReady");
  },
});