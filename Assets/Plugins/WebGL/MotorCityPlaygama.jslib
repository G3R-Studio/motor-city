mergeInto(LibraryManager.library, {
  MotorCityRankingSubmit: function(idPtr, score) {
    var id = UTF8ToString(idPtr);
    var b = window.bridge && window.bridge.leaderboards;
    if (!b || b.type === 'not_available' || !isFinite(score) || score <= 0) return;
    var state = window.motorCityRankingSent || (window.motorCityRankingSent = {});
    var previous = state[id];
    if (previous && (previous.pending || (id === 'race_time' ? previous.score <= score : previous.score >= score))) return;
    state[id] = {pending:true, score:score};
    Promise.resolve().then(function(){return b.setScore(id,score);}).then(function(){state[id].pending=false;},function(){if(previous) state[id]=previous; else delete state[id];});
  },
  MotorCityRankingOpen: function(idPtr) {
    var initial = UTF8ToString(idPtr);
    var b = window.bridge && window.bridge.leaderboards;
    if (!b || b.type === 'not_available' || b.type === 'native') return;
    if (b.type === 'native_popup') { Promise.resolve().then(function(){return b.showNativePopup(initial);}).catch(function(){}); return; }
    if (document.getElementById('motorcity-ranking')) return;
    var ru = window.bridge.platform.language === 'ru';
    var ids = ['reputation','drift_score','race_time','completed_jobs','distance'];
    var names = ru ? ['Лучшие водители','Короли дрифта','Лучшие гонщики','Профессионалы','Активные водители'] : ['Best drivers','Drift kings','Best racers','Professionals','Active drivers'];
    var overlay = document.createElement('div');
    overlay.id = 'motorcity-ranking';
    overlay.style.cssText = 'position:fixed;inset:0;z-index:2147483646;background:rgba(5,8,18,.94);color:white;display:flex;align-items:center;justify-content:center;font:16px Arial,sans-serif;touch-action:auto';
    var panel = document.createElement('div');
    panel.style.cssText = 'width:min(90vw,760px);max-height:85vh;overflow:auto;padding:20px;background:#131c30;border:1px solid #36c9ff;border-radius:12px;box-sizing:border-box';
    overlay.appendChild(panel);
    var title = document.createElement('h2'); title.textContent = ru?'Рейтинг игроков':'Leaderboards'; panel.appendChild(title);
    var select = document.createElement('select'); select.style.cssText='max-width:100%;padding:10px;background:#202d44;color:white;font:inherit';
    ids.forEach(function(id,i){var o=document.createElement('option');o.value=id;o.textContent=names[i];select.appendChild(o);}); select.value=initial;panel.appendChild(select);
    var close=document.createElement('button');close.textContent=ru?'Закрыть':'Close';close.style.cssText='margin:8px;padding:10px;background:#283952;color:white;border:1px solid #36c9ff;border-radius:6px;font:inherit';panel.appendChild(close);
    var list=document.createElement('div');list.style.cssText='margin-top:12px;min-height:70px';panel.appendChild(list);
    var generation=0;
    function refresh(){
      var seq=++generation,id=select.value;list.textContent=ru?'Загрузка...':'Loading...';
      Promise.resolve().then(function(){return b.getEntries(id);}).then(function(entries){
        if(seq!==generation||!overlay.isConnected)return;
        list.textContent='';
        if(!Array.isArray(entries)||!entries.length){list.textContent=ru?'Пока нет результатов':'No scores yet';return;}
        entries.slice(0,100).forEach(function(e){
          var row=document.createElement('div');row.style.cssText='display:flex;gap:12px;justify-content:space-between;padding:10px 4px;border-bottom:1px solid #33415a';
          var name=document.createElement('span');name.textContent=String(e.rank||'—')+'. '+String(e.name|| (ru?'Игрок':'Player'));name.style.overflowWrap='anywhere';
          var value=document.createElement('span');value.textContent=id==='race_time'?(Number(e.score)/1000).toFixed(2)+' s':id==='distance'?(Number(e.score)/1000).toFixed(2)+' km':String(e.score);
          row.appendChild(name);row.appendChild(value);list.appendChild(row);
        });
      }).catch(function(){if(seq===generation)list.textContent=ru?'Ошибка загрузки. Выберите рейтинг повторно.':'Loading failed. Select a board again.';});
    }
    close.onclick=function(){generation++;overlay.remove();};
    select.onchange=refresh;
    var retry=document.createElement('button');retry.textContent=ru?'Обновить':'Refresh';retry.onclick=refresh;panel.insertBefore(retry,list);
    document.body.appendChild(overlay);refresh();
  },
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
