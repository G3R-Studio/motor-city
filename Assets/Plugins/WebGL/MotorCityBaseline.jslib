mergeInto(LibraryManager.library, {
  MotorCityBaselineInstall: function() {
    window.motorCityBaseline = {
      request: 0, report: '',
      start: function() { this.request = 1; },
      stop: function() { this.request = 2; },
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
