/// <reference path="require.js" />


requirejs(['ui/screen'], function(screen) {
    var disp = screen.displayVersion;
    document.getElementById("out").innerHTML = disp;
});