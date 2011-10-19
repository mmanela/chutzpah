/// <reference path="qunit.js" />
/// <reference path="coffee.js" />
/*globals describe, it, expect, jasmine, coffee*/

(function () {
    'use strict';

    module('coffee machine');

    test('makes a single shot flat white given milk and 1 shot', function () {
        var actual = coffee.machine(1, 'milk', false),
            expected = 'single shot flat white';
        equal(actual, expected);
    });

    test('makes a double shot cafe latte given froth, 2 shots and sugar', function () {
        var actual = coffee.machine(2, 'froth', true),
            expected = 'double shot cafe latte';
        equal(actual, expected);
    });
}());