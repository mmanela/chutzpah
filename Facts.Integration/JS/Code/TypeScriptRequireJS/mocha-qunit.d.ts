// Type definitions for Mocha-Qunit

declare function asyncTest(name: string, test: () => any);

declare function suite(name: string, testEnvironment?: any);

declare function test(title: string, test: () => any);