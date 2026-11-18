mergeInto(LibraryManager.library, {
  $MotorCityMusic: {
    current: null, next: null, url: '', nextUrl: '', retry: 0, pending: false,
    clear: function(a) {
      if (!a) return;
      a.pause(); a.removeAttribute('src'); a.load();
    },
    stop: function() {
      MotorCityMusic.clear(MotorCityMusic.current);
      MotorCityMusic.clear(MotorCityMusic.next);
      MotorCityMusic.current = MotorCityMusic.next = null;
      MotorCityMusic.url = MotorCityMusic.nextUrl = '';
      MotorCityMusic.pending = false;
    }
  },
  MotorCityMusicTick__deps: ['$MotorCityMusic'],
  MotorCityMusicTick: function(urlPtr, nextPtr, active, paused, volume) {
    var m = MotorCityMusic, url = UTF8ToString(urlPtr), nextUrl = UTF8ToString(nextPtr);
    if (!active || !url) { m.stop(); return; }
    if (m.url !== url) {
      m.clear(m.current);
      if (m.next && m.nextUrl === url) {
        m.current = m.next; m.next = null; m.nextUrl = '';
      } else {
        m.current = new Audio(url);
      }
      m.url = url; m.pending = false; m.retry = 0;
      m.current.preload = 'auto';
    }
    var a = m.current;
    a.volume = Math.max(0, Math.min(1, volume));
    if (paused) { a.pause(); return; }
    if (a.paused && !a.ended && !a.error && !m.pending && Date.now() >= m.retry) {
      m.pending = true;
      Promise.resolve(a.play()).then(function() {
        if (m.current === a) { m.pending = false; }
      }).catch(function(e) {
        if (m.current === a) {
          m.pending = false; m.retry = Date.now()+1000;
          console.warn('Motor City music play:', e);
        }
      });
    }
    if (nextUrl && nextUrl !== url && m.nextUrl !== nextUrl) {
      m.clear(m.next);
      m.next = new Audio(); m.next.preload = 'auto'; m.next.src = nextUrl;
      m.nextUrl = nextUrl; m.next.load();
    }
  },
  MotorCityMusicState__deps: ['$MotorCityMusic'],
  MotorCityMusicState: function() {
    var a = MotorCityMusic.current;
    return !a ? 0 : a.error ? 3 : a.ended ? 2 : 1;
  },
  MotorCityMusicStop__deps: ['$MotorCityMusic'],
  MotorCityMusicStop: function() { MotorCityMusic.stop(); }
});
