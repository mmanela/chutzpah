/// <reference path="jasmine.js" />
/// <reference path="coffee.js" />
/*globals describe, it, expect, jasmine, coffee*/

(function () {
    'use strict';

    describe('coffee', function () {
        describe('machine', function () {
            it('makes a single shot flat white given milk and 1 shot', function () {
                waits(5000);
                runs(function () {
                    var actual = coffee.machine(1, 'milk', false),
                        expected = 'single shot flat white';
                    expect(actual).toEqual(expected);
                });
            });
            it('makes a double shot cafe latte given froth, 2 shots and sugar', function () {
                var actual = coffee.machine(2, 'froth', true),
                    expected = 'double shot cafe latte';
                expect(actual).toEqual(expected);
            });
        });
    });
}());