import {Component} from 'angular2/core';

@Component({ selector: 'node', templateUrl: 'views/node.component.html' })
export class NodeComponent {
    nodes = new Array<Node>();

    constructor() {
        this.nodes.push(new Node('Home'));

        var node = new Node('About Us');
        node.children.push(new Node('Careers'));
        this.nodes.push(node);
    }
}

export class Node {
    text: string;
    children = new Array<Node>();

    constructor(text: string) {
        this.text = text;
    }

    hasChildren() {
        return this.children.length > 0;
    }
}