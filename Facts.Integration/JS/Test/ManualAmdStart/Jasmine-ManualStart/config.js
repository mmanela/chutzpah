window.chutzpah.preventAutoStart();

setTimeout(function() {
  require.config({
      
      paths: {
          hello: 'base/jquery.hello',
      },

      shim: {
          hello: { deps: ["jquery"] },
      }
  });
  
  window.chutzpah.start();
}, 500);