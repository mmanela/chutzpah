import {Node} from './node.component'

describe('Node', () => {
    let menuItem = new Node('Node1');

    it('does not have children if it does not contain sub-nodes.', () => {
        expect(menuItem.hasChildren()).toBeFalsy();
    });

    it('has children if it contains sub-nodes.', () => {
        menuItem.children.push(new Node('ChildNode1'));
        expect(menuItem.hasChildren()).toBeTruthy();
    });
});