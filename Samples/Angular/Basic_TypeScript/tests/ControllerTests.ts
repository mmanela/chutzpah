/// <reference path="../src/controller.ts" />
/// <reference path="../../libs/typings/angularjs/angular-mocks.d.ts" />
/// <reference path="../../libs/typings/angularjs/angular.d.ts" />
/// <reference path="../../libs/typings/jasmine/jasmine.d.ts" />


namespace Tests {
    describe("ControllerTests", () => {
        
        var $http: angular.IHttpService;
        var $httpBackend: angular.IHttpBackendService;
        var controller: Example.Controller;
        
        beforeEach(() => {
            angular.mock.inject((_$http_: angular.IHttpService, _$httpBackend_: angular.IHttpBackendService) => {
                // The injector unwraps the underscores (_) from around the parameter names when matching
                $http = _$http_;
                $httpBackend = _$httpBackend_;
                controller = new Example.Controller($http);
            });
        });

        it("should run tests", () => {
            $httpBackend.whenGET("someAddress").respond(200, 1);

            controller.DoSomeThing();
            $httpBackend.flush();

            expect(controller.someResponse).toBe(1);
        });
    });
}