window.chutzpah = window.chutzpah || {};

window.chutzpah.autoStart = true;

window.chutzpah.preventAutoStart =  function () {
    window.chutzpah.autoStart = false;
};

window.chutzpah.boot = function (amdTestPaths) {

    var amdImport, amdConfig;

    // Are we requireJS compatible?
    if (window.require && typeof window.require === "function") {
        amdImport = function (paths, callback) {
            return window.require.apply(window.require, [paths, callback]);
        }
        amdConfig = function () {
            return window.require.config.apply(window.require, arguments);
        }
    }
    // Are we systemJS compatible?
    else if (window.System && typeof window.System.import === "function") {
        amdImport = function (paths, callback) {
            var imports = paths.map(function(path) { 
                return window.System.import(path); 
            });
            return Promise.all(imports).then(callback);
        }

        amdConfig = function () {
            return window.System.config.apply(window.System, arguments);
        }
    }
    window.chutzpah.amdImport = amdImport;
    window.chutzpah.amdConfig = amdConfig;

    if (window.chutzpah.amdImport && amdTestPaths.length > 0) {
        console.log("!!_!! Test file is using module loader.");
        window.chutzpah.usingModuleLoader = true;
    }
}