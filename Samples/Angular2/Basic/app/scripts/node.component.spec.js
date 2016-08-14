System.register(['./node.component'], function(exports_1, context_1) {
    "use strict";
    var __moduleName = context_1 && context_1.id;
    var node_component_1;
    return {
        setters:[
            function (node_component_1_1) {
                node_component_1 = node_component_1_1;
            }],
        execute: function() {
            describe('Node', function () {
                var menuItem = new node_component_1.Node('Node1');
                it('does not have children if it does not contain sub-nodes.', function () {
                    expect(menuItem.hasChildren()).toBeFalsy();
                });
                it('has children if it contains sub-nodes.', function () {
                    menuItem.children.push(new node_component_1.Node('ChildNode1'));
                    expect(menuItem.hasChildren()).toBeTruthy();
                });
            });
        }
    }
});
//# sourceMappingURL=node.component.spec.js.map