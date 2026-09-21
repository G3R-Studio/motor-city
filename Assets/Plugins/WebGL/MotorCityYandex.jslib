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
  },
  MotorCityYandexSubmitLeaderboard: function(gameObjectNamePtr, leaderboardPtr, score) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var leaderboard = UTF8ToString(leaderboardPtr);
    var ysdk = window.MotorCityYandexSdk;
    function send(v) { SendMessage(gameObjectName, 'OnYandexLeaderboardResult', v ? '1' : '0'); }

    if (!ysdk || !ysdk.leaderboards) { send(false); return; }

    Promise.resolve(
      typeof ysdk.isAvailableMethod === 'function'
        ? ysdk.isAvailableMethod('leaderboards.setScore')
        : true)
      .then(function(available) {
        if (!available) throw new Error('leaderboards.setScore unavailable');
        return ysdk.leaderboards.setScore(leaderboard, Number(score));
      })
      .then(function() { send(true); })
      .catch(function(error) {
        console.warn('Motor City: leaderboard submit failed', error);
        send(false);
      });
  },

  MotorCityYandexShowRewarded: function(gameObjectNamePtr, placementPtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var ysdk = window.MotorCityYandexSdk;
    var rewarded = false;

    if (!ysdk || !ysdk.adv) {
      SendMessage(gameObjectName, 'OnYandexRewardedResult', '0');
      return;
    }

    ysdk.adv.showRewardedVideo({
      callbacks: {
        onRewarded: function() { rewarded = true; },
        onClose: function() {
          SendMessage(gameObjectName, 'OnYandexRewardedResult', rewarded ? '1' : '0');
        },
        onError: function(error) {
          console.warn('Motor City: rewarded ad failed', error);
          SendMessage(gameObjectName, 'OnYandexRewardedResult', '0');
        }
      }
    });
  },

  MotorCityYandexShowInterstitial: function(gameObjectNamePtr, placementPtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var ysdk = window.MotorCityYandexSdk;
    var done = false;
    function close() {
      if (done) return;
      done = true;
      SendMessage(gameObjectName, 'OnYandexInterstitialClosed', '1');
    }

    if (!ysdk || !ysdk.adv) { close(); return; }

    ysdk.adv.showFullscreenAdv({
      callbacks: {
        onClose: close,
        onError: function(error) {
          console.warn('Motor City: interstitial failed', error);
          close();
        }
      }
    });
  },

  MotorCityYandexPurchase: function(gameObjectNamePtr, productIdPtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var productId = UTF8ToString(productIdPtr);
    var ysdk = window.MotorCityYandexSdk;

    if (!ysdk || !ysdk.payments) {
      SendMessage(gameObjectName, 'OnYandexPurchaseResult', '0|');
      return;
    }

    ysdk.payments.purchase({ id: productId })
      .then(function(purchase) {
        var token = purchase && purchase.purchaseToken ? purchase.purchaseToken : '';
        SendMessage(gameObjectName, 'OnYandexPurchaseResult', '1|' + token);
      })
      .catch(function(error) {
        console.warn('Motor City: purchase failed', error);
        SendMessage(gameObjectName, 'OnYandexPurchaseResult', '0|');
      });
  },

  MotorCityYandexConsumePurchase: function(gameObjectNamePtr, tokenPtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var token = UTF8ToString(tokenPtr);
    var ysdk = window.MotorCityYandexSdk;

    if (!ysdk || !ysdk.payments) {
      SendMessage(gameObjectName, 'OnYandexConsumeResult', '0');
      return;
    }

    ysdk.payments.consumePurchase(token)
      .then(function() {
        SendMessage(gameObjectName, 'OnYandexConsumeResult', '1');
      })
      .catch(function(error) {
        console.warn('Motor City: consume purchase failed', error);
        SendMessage(gameObjectName, 'OnYandexConsumeResult', '0');
      });
  },

  MotorCityYandexLoadPendingPurchases: function(gameObjectNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var ysdk = window.MotorCityYandexSdk;

    if (!ysdk || !ysdk.payments) {
      SendMessage(gameObjectName, 'OnYandexPendingPurchasesFailed', 'Payments unavailable');
      return;
    }

    ysdk.payments.getPurchases()
      .then(function(purchases) {
        var compact = (purchases || []).map(function(p) {
          return encodeURIComponent(p.productID || '') + ':' + encodeURIComponent(p.purchaseToken || '');
        }).join(';');
        SendMessage(gameObjectName, 'OnYandexPendingPurchases', compact);
      })
      .catch(function(error) {
        SendMessage(gameObjectName, 'OnYandexPendingPurchasesFailed',
          error && error.message ? error.message : error);
      });
  },

  MotorCityYandexLoadRemoteConfig: function(gameObjectNamePtr) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var ysdk = window.MotorCityYandexSdk;

    if (!ysdk || typeof ysdk.getFlags !== 'function') {
      SendMessage(gameObjectName, 'OnYandexRemoteConfigFailed', 'Remote config unavailable');
      return;
    }

    Promise.resolve(ysdk.getFlags({
      defaultFlags: {
        rewarded_credits: '250',
        interstitial_min_seconds: '240',
        daily_tasks_enabled: '1'
      }
    }))
      .then(function(flags) {
        var compact = Object.keys(flags || {}).map(function(key) {
          return encodeURIComponent(key) + '=' + encodeURIComponent(String(flags[key]));
        }).join('&');
        SendMessage(gameObjectName, 'OnYandexRemoteConfig', compact);
      })
      .catch(function(error) {
        SendMessage(gameObjectName, 'OnYandexRemoteConfigFailed',
          error && error.message ? error.message : error);
      });
  },

  MotorCityYandexIncrementStat: function(gameObjectNamePtr, keyPtr, amount) {
    var gameObjectName = UTF8ToString(gameObjectNamePtr);
    var key = UTF8ToString(keyPtr);
    var player = window.MotorCityYandexPlayer;

    if (!player || typeof player.incrementStats !== 'function') {
      SendMessage(gameObjectName, 'OnYandexStatResult', '0');
      return;
    }

    var data = {};
    data[key] = Number(amount);

    player.incrementStats(data)
      .then(function() {
        SendMessage(gameObjectName, 'OnYandexStatResult', '1');
      })
      .catch(function(error) {
        console.warn('Motor City: incrementStats failed', error);
        SendMessage(gameObjectName, 'OnYandexStatResult', '0');
      });
  }
});
