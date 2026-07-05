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
        var state = globalThis.WGLRhythmLocalMp3 || {};
        if (state.audio) {
          state.audio.pause();
          state.audio.src = "";
        }
        if (state.source) {
          state.source.disconnect();
        }
        if (state.analyser) {
          state.analyser.disconnect();
        }

        var AudioContextCtor = window.AudioContext || window.webkitAudioContext;
        if (!AudioContextCtor) {
          SendMessage(targetObjectName, "OnLocalMp3SelectionFailed", "Web Audio API is not available in this browser.");
          URL.revokeObjectURL(blobUrl);
          return;
        }

        state.audioContext = state.audioContext || new AudioContextCtor();
        state.audio = new Audio();
        state.audio.preload = "auto";
        state.audio.src = blobUrl;
        state.analyser = state.audioContext.createAnalyser();
        state.analyser.fftSize = 2048;
        state.analyser.smoothingTimeConstant = 0.82;
        state.frequencyData = new Uint8Array(state.analyser.frequencyBinCount);
        state.source = state.audioContext.createMediaElementSource(state.audio);
        state.source.connect(state.analyser);
        state.analyser.connect(state.audioContext.destination);
        state.isPlaying = false;
        state.audio.onended = function () {
          state.isPlaying = false;
          SendMessage(targetObjectName, "OnLocalMp3Ended", "");
        };
        globalThis.WGLRhythmLocalMp3 = state;

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
  },

  PlayLocalMp3File: function () {
    var state = globalThis.WGLRhythmLocalMp3;
    if (!state || !state.audio) {
      return;
    }

    var play = function () {
      var promise = state.audio.play();
      if (promise && promise.catch) {
        promise.catch(function (error) {
          console.error(error);
        });
      }
      state.isPlaying = true;
    };

    if (state.audioContext && state.audioContext.state === "suspended") {
      state.audioContext.resume().then(play);
    } else {
      play();
    }
  },

  PauseLocalMp3File: function () {
    var state = globalThis.WGLRhythmLocalMp3;
    if (!state || !state.audio) {
      return;
    }

    state.audio.pause();
    state.isPlaying = false;
  },

  StopLocalMp3File: function () {
    var state = globalThis.WGLRhythmLocalMp3;
    if (!state || !state.audio) {
      return;
    }

    state.audio.pause();
    state.audio.currentTime = 0;
    state.isPlaying = false;
  },

  IsLocalMp3Playing: function () {
    var state = globalThis.WGLRhythmLocalMp3;
    return state && state.audio && state.isPlaying ? 1 : 0;
  },

  GetLocalMp3Spectrum: function (bufferPtr, length) {
    var state = globalThis.WGLRhythmLocalMp3;
    if (!state || !state.analyser || !state.frequencyData || !state.isPlaying) {
      return 0;
    }

    state.analyser.getByteFrequencyData(state.frequencyData);

    var heap = HEAPF32;
    var offset = bufferPtr >> 2;
    var bins = state.frequencyData.length;
    var peak = 0;

    var frequencyCurve = 1.15;
    var highFrequencyBoost = 1.8;

    for (var i = 0; i < length; i++) {
      var start = Math.floor(Math.pow(i / length, frequencyCurve) * bins);
      var end = Math.floor(Math.pow((i + 1) / length, frequencyCurve) * bins);
      end = Math.max(start + 1, Math.min(end, bins));

      var sum = 0;
      for (var j = start; j < end; j++) {
        sum += state.frequencyData[j];
      }

      var average = sum / (end - start);
      var bandPosition = length <= 1 ? 0 : i / (length - 1);
      var bandGain = 1 + (highFrequencyBoost - 1) * bandPosition;
      var normalized = average / 255 * bandGain * 0.0015;
      heap[offset + i] = normalized;
      if (normalized > peak) {
        peak = normalized;
      }
    }

    return peak;
  }
});
