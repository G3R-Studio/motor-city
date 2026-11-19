mergeInto(LibraryManager.library, {
  MotorCityWebConfigureBrowser: function() {
    try {
      var canvas =
        (typeof Module !== 'undefined' && Module.canvas)
          ? Module.canvas
          : document.querySelector('canvas');

      if (!canvas || canvas.dataset.motorCityBrowserConfigured === '1') {
        return;
      }

      canvas.dataset.motorCityBrowserConfigured = '1';

      // Layout is set in the generated index.html by the WebGL build
      // postprocessor, before createUnityInstance is called. Do not change
      // container/canvas sizing here after Unity has initialized.

      // Keep all gestures inside the Unity surface. Web portals require that
      // mobile gameplay does not trigger browser scrolling, pull-to-refresh,
      // text selection or long-press browser UI.
      canvas.style.touchAction = 'none';
      canvas.style.userSelect = 'none';
      canvas.style.webkitUserSelect = 'none';
      canvas.style.webkitTouchCallout = 'none';

      if (document.documentElement) {
        document.documentElement.style.overscrollBehavior = 'none';
        document.documentElement.style.overflow = 'hidden';
      }

      if (document.body) {
        document.body.style.overscrollBehavior = 'none';
        document.body.style.overflow = 'hidden';
        document.body.style.margin = '0';
      }

      canvas.addEventListener('contextmenu', function(event) {
        event.preventDefault();
      }, false);

      canvas.addEventListener('dragstart', function(event) {
        event.preventDefault();
      }, false);

      canvas.addEventListener('selectstart', function(event) {
        event.preventDefault();
      }, false);
    } catch (error) {
      console.warn('Motor City: browser canvas configuration failed', error);
    }
  },

  MotorCityBridgeInitialize: function(namePtr) {
    var name = UTF8ToString(namePtr);
    var env = window.motorCityBridge;
    function send(method, value) { SendMessage(name, method, String(value)); }
    if (!env || !env.initialized) { send('OnBridgeInitFailed', 'Bridge not initialized'); return; }
    env.notifyPause = function(value) { send('OnBridgePause', value ? '1' : '0'); };
    env.notifyAudio = function(value) { send('OnBridgeAudio', value ? '1' : '0'); };
    env.notifyPause(env.paused || document.hidden);
    env.notifyAudio(env.audioEnabled);
    send('OnBridgeInitialized', env.language + '|' +
      (bridge.advertisement.isRewardedSupported ? '1' : '0') + '|' +
      (bridge.advertisement.isInterstitialSupported ? '1' : '0'));
  },

  MotorCityBridgeMessage: function(messagePtr) {
    try { Promise.resolve(bridge.platform.sendMessage(UTF8ToString(messagePtr))).catch(console.warn); }
    catch (error) { console.warn('Motor City: platform message failed', error); }
  },

  MotorCityBridgeAd: function(namePtr, placementPtr, rewarded) {
    var name = UTF8ToString(namePtr);
    var placement = UTF8ToString(placementPtr);
    var env = window.motorCityBridge;
    var adv = bridge.advertisement;
    var type = rewarded ? 'rewarded' : 'interstitial';
    var callback = rewarded ? 'OnBridgeRewarded' : 'OnBridgeInterstitial';
    var stateKey = rewarded ? 'rewardedState' : 'interstitialState';
    var supported = rewarded ? adv.isRewardedSupported : adv.isInterstitialSupported;
    function send(value) { SendMessage(name, callback, value ? '1' : '0'); }
    if (!supported || env.adRequest ||
        ['loading','opened'].indexOf(adv.interstitialState) !== -1 ||
        ['loading','opened','rewarded'].indexOf(adv.rewardedState) !== -1) { send(false); return; }
    var request = { type: type, earned: false, opened: false, finish: null };
    var watchdog;
    request.finish = function() {
      if (env.adRequest !== request) return;
      env.adRequest = null;
      clearTimeout(watchdog);
      send(request.earned);
    };
    env.adRequest = request;
    // Recovery for an adapter which never emits a state, not an ad-frequency timer.
    // Never resume gameplay during an opened ad.
    watchdog = setTimeout(function() {
      if (!request.opened && adv[stateKey] !== 'opened' && adv[stateKey] !== 'rewarded') request.finish();
    }, 20000);
    try {
      if (rewarded) adv.showRewarded(placement);
      else adv.showInterstitial(placement);
      // Some adapters suppress a duplicate FAILED event when an attempt is throttled.
      if (env.adRequest === request && adv[stateKey] === 'failed') request.finish();
    } catch (error) {
      console.warn('Motor City: Bridge ad request failed', error);
      request.finish();
    }
  },

  MotorCityBridgePrefHas: function(keyPtr) {
    return Object.prototype.hasOwnProperty.call(window.motorCityBridge.prefs, UTF8ToString(keyPtr)) ? 1 : 0;
  },
  MotorCityBridgePrefGet: function(keyPtr) {
    var value = window.motorCityBridge.prefs[UTF8ToString(keyPtr)];
    var str = value == null ? '' : String(value);
    var size = lengthBytesUTF8(str) + 1;
    var buffer = _malloc(size);
    stringToUTF8(str, buffer, size);
    return buffer;
  },
  MotorCityBridgePrefSet: function(keyPtr, valuePtr) {
    window.motorCityBridge.prefs[UTF8ToString(keyPtr)] = UTF8ToString(valuePtr);
  },
  MotorCityBridgePrefDelete: function(keyPtr) {
    delete window.motorCityBridge.prefs[UTF8ToString(keyPtr)];
  },
  MotorCityBridgePrefFlush: function() {
    window.motorCityBridge.flush();
  }
});
