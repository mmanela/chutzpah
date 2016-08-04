System.register(['angular2/platform/browser', './node.component'], function(exports_1, context_1) {
    "use strict";
    var __moduleName = context_1 && context_1.id;
    var browser_1, node_component_1;
    return {
        setters:[
            function (browser_1_1) {
                browser_1 = browser_1_1;
            },
            function (node_component_1_1) {
                node_component_1 = node_component_1_1;
            }],
        execute: function() {
            browser_1.bootstrap(node_component_1.NodeComponent);
        }
    }
});
//# sourceMappingURL=boot.js.map