/// <reference path="scripts/typings/angularjs/angular-mocks.d.ts" />
/// <reference path="scripts/typings/angularjs/angular.d.ts" />
declare namespace Example {
    class Controller {
        private $http;
        someResponse: number;
        constructor($http: angular.IHttpService);
        DoSomeThing: () => void;
    }
}
