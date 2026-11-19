mergeInto(LibraryManager.library, {
  MotorCityBaselineInstall: function() {
    window.motorCityBaseline = {
      request: 0, report: '',
      start: function() { this.report = ''; this.request = 1; console.log('Baseline armed: return focus to the game. Starts after at least 5 seconds.'); },
      stop: function() { this.request = 2; },
      meshPilot: function() { this.request = 3; },
      save: function() {
        if (!this.report) return;
        var u = URL.createObjectURL(new Blob([this.report], {type:'text/plain'}));
        var a = document.createElement('a');
        a.href = u; a.download = 'MotorCityWebBaseline.txt'; a.click();
        setTimeout(function(){ URL.revokeObjectURL(u); }, 1000);
      }
    };
  },
  MotorCityBaselineRequest: function() {
    var b = window.motorCityBaseline;
    if (!b) return 0;
    var r = b.request; b.request = 0; return r;
  },
  MotorCityBaselinePublish: function(ptr) {
    var b = window.motorCityBaseline;
    if (!b) return;
    b.report = UTF8ToString(ptr) + '\nBrowser=' + navigator.userAgent + '\nDPR=' + window.devicePixelRatio;
    console.log(b.report);
    console.log('Save: motorCityBaseline.save()');
  }
});
