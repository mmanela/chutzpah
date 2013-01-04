function assertFileLoad(file) {
    stop();
    var scriptLoaded = function () {
        ok(window.loadMe);
        start();
    };

    var headNode = document.getElementsByTagName("head")[0];
    var scriptNode = document.createElement('script');
    scriptNode.type = 'text/javascript';
    scriptNode.onload = scriptLoaded;
    scriptNode.src = file;
    headNode.appendChild(scriptNode);
}