window.chutzpah.preventAmdAutoStart();

setTimeout(function() {
  require.config({
      
      paths: {
          hello: 'base/jquery.hello',
      },

      shim: {
          hello: { deps: ["jquery"] },
      }
  });
  
  window.chutzpah.amdStart();
}, 500);