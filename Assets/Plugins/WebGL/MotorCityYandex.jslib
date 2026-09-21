mergeInto(LibraryManager.library, {
  MotorCityYandexInitialize: function(gameObjectNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);

    function send(method, value) {
      if (typeof SendMessage === 'function') {
        SendMessage(gameObjectName, method, value == null ? '' : String(value));
      }
    }

    async function initializeSdk() {
      try {
        if (typeof YaGames === 'undefined') {
          throw new Error('YaGames is not defined after /sdk.js load');
        }

        var ysdk = await YaGames.init();
        window.MotorCityYandexSdk = ysdk;

        var player = null;
        try {
          player = await ysdk.getPlayer();
        } catch (playerError) {
          console.warn('Motor City: getPlayer failed', playerError);
        }

        window.MotorCityYandexPlayer = player;

        var authorized = 0;
        if (player && typeof player.isAuthorized === 'function') {
          authorized = player.isAuthorized() ? 1 : 0;
        }

        var language = 'ru';
        if (ysdk.environment &&
            ysdk.environment.i18n &&
            ysdk.environment.i18n.lang) {
          language = ysdk.environment.i18n.lang;
        }

        var serverTime = Date.now();
        if (typeof ysdk.serverTime === 'function') {
          serverTime = ysdk.serverTime();
        }

        var playerAvailable = player ? 1 : 0;

        send('OnYandexInitialized',
          authorized + '|' + language + '|' + Math.floor(serverTime) + '|' + playerAvailable);
      } catch (error) {
        send('OnYandexInitFailed', error && error.message ? error.message : error);
      }
    }

    if (typeof YaGames !== 'undefined') {
      initializeSdk();
      return;
    }

    var existing = document.querySelector('script[data-motor-city-yandex-sdk="1"]');
    if (existing) {
      existing.addEventListener('load', initializeSdk, { once: true });
      existing.addEventListener('error', function() {
        send('OnYandexInitFailed', 'Failed to load /sdk.js');
      }, { once: true });
      return;
    }

    var script = document.createElement('script');
    script.src = '/sdk.js';
    script.async = true;
    script.dataset.motorCityYandexSdk = '1';
    script.onload = initializeSdk;
    script.onerror = function() {
      send('OnYandexInitFailed', 'Failed to load /sdk.js');
    };
    document.head.appendChild(script);
  },

  MotorCityYandexGameReady: function() {
    var ysdk = window.MotorCityYandexSdk;
    try {
      if (ysdk && ysdk.features && ysdk.features.LoadingAPI) {
        ysdk.features.LoadingAPI.ready();
      }
    } catch (error) {
      console.warn('Motor City: LoadingAPI.ready failed', error);
    }
  },

  MotorCityYandexGameplayStart: function() {
    var ysdk = window.MotorCityYandexSdk;
    try {
      if (ysdk && ysdk.features && ysdk.features.GameplayAPI) {
        ysdk.features.GameplayAPI.start();
      }
    } catch (error) {
      console.warn('Motor City: GameplayAPI.start failed', error);
    }
  },

  MotorCityYandexGameplayStop: function() {
    var ysdk = window.MotorCityYandexSdk;
    try {
      if (ysdk && ysdk.features && ysdk.features.GameplayAPI) {
        ysdk.features.GameplayAPI.stop();
      }
    } catch (error) {
      console.warn('Motor City: GameplayAPI.stop failed', error);
    }
  },

  MotorCityYandexServerTime: function() {
    var ysdk = window.MotorCityYandexSdk;

    try {
      if (ysdk && typeof ysdk.serverTime === 'function') {
        return ysdk.serverTime();
      }
    } catch (error) {
      console.warn('Motor City: serverTime failed', error);
    }

    return Date.now();
  },

  MotorCityYandexLoadCloudSave: function(gameObjectNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var player = window.MotorCityYandexPlayer;

    function send(method, value) {
      if (typeof SendMessage === 'function') {
        SendMessage(gameObjectName, method, value == null ? '' : String(value));
      }
    }

    if (!player || typeof player.getData !== 'function') {
      send('OnYandexCloudLoadFailed', 'Player Data is unavailable');
      return;
    }

    player.getData(['motorCitySaveJson'])
      .then(function(data) {
        var json = data && typeof data.motorCitySaveJson === 'string'
          ? data.motorCitySaveJson
          : '';

        send('OnYandexCloudLoaded', json);
      })
      .catch(function(error) {
        send('OnYandexCloudLoadFailed',
          error && error.message ? error.message : error);
      });
  },

  MotorCityYandexSaveCloudSave: function(gameObjectNamePtr, jsonPtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var json = UTF8ToString(jsonPtr);
    var player = window.MotorCityYandexPlayer;

    function send(method, value) {
      if (typeof SendMessage === 'function') {
        SendMessage(gameObjectName, method, value == null ? '' : String(value));
      }
    }

    if (!player || typeof player.setData !== 'function') {
      send('OnYandexCloudSaveFailed', 'Player Data is unavailable');
      return;
    }

    player.setData({
      motorCitySaveJson: json
    }, true)
      .then(function() {
        send('OnYandexCloudSaved', '1');
      })
      .catch(function(error) {
        send('OnYandexCloudSaveFailed',
          error && error.message ? error.message : error);
      });
  }
});
