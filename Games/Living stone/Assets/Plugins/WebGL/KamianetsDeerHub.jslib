mergeInto(LibraryManager.library, {
  KamianetsDeerPostComplete: function () {
    try {
      if (typeof window !== "undefined" && window.parent) {
        window.parent.postMessage(
          { type: "kamianets-deer", status: "completed", score: 100 },
          "*"
        );
      }
    } catch (e) {
      console.warn("KamianetsDeerPostComplete failed", e);
    }
  },
});
