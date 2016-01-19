/// <reference path="../src/controller.ts" />
/// <reference path="../src/scripts/typings/angularjs/angular-mocks.d.ts" />
/// <reference path="../src/scripts/typings/angularjs/angular.d.ts" />
/// <reference path="scripts/typings/jasmine/jasmine.d.ts" />
var Tests;
(function (Tests) {
    describe("ControllerTests", function () {
        var $http;
        var $httpBackend;
        var controller;
        beforeEach(function () {
            angular.mock.inject(function (_$http_, _$httpBackend_) {
                // The injector unwraps the underscores (_) from around the parameter names when matching
                $http = _$http_;
                $httpBackend = _$httpBackend_;
                controller = new Example.Controller($http);
            });
        });
        it("should run tests", function () {
            $httpBackend.whenGET("someAddress").respond(200, 1);
            controller.DoSomeThing();
            $httpBackend.flush();
            expect(controller.someResponse).toBe(1);
        });
    });
})(Tests || (Tests = {}));
//# sourceMappingURL=ControllerTests.js.map