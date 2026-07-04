mergeInto(LibraryManager.library, {
  OpenLocalMp3File: function (targetObjectNamePtr) {
    var targetObjectName = UTF8ToString(targetObjectNamePtr);
    var input = document.createElement("input");
    input.type = "file";
    input.accept = "audio/mpeg,audio/mp3,.mp3";
    input.style.display = "none";

    input.onchange = function () {
      try {
        var file = input.files && input.files.length > 0 ? input.files[0] : null;
        if (!file) {
          SendMessage(targetObjectName, "OnLocalMp3SelectionCanceled", "Local MP3 selection canceled.");
          return;
        }

        var blobUrl = URL.createObjectURL(file);
        SendMessage(targetObjectName, "OnLocalMp3Selected", blobUrl + "\n" + file.name);
      } catch (error) {
        SendMessage(targetObjectName, "OnLocalMp3SelectionFailed", String(error));
      } finally {
        if (input.parentNode) {
          input.parentNode.removeChild(input);
        }
      }
    };

    document.body.appendChild(input);
    input.click();
  },

  RevokeLocalMp3Url: function (blobUrlPtr) {
    var blobUrl = UTF8ToString(blobUrlPtr);
    if (blobUrl) {
      URL.revokeObjectURL(blobUrl);
    }
  }
});
