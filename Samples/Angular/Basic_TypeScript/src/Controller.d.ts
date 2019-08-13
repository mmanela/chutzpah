/// <reference path="../../libs/typings/angularjs/angular-mocks.d.ts" />
/// <reference path="../../libs/typings/angularjs/angular.d.ts" />
declare namespace Example {
    class Controller {
        private $http;
        someResponse: number;
        constructor($http: angular.IHttpService);
        DoSomeThing: () => void;
    }
}
