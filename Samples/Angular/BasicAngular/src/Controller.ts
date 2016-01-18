/// <reference path="scripts/typings/angularjs/angular-mocks.d.ts" />
/// <reference path="scripts/typings/angularjs/angular.d.ts" />

namespace Example {
    export class Controller {
        private $http: angular.IHttpService
        public someResponse: number;
        constructor($http: angular.IHttpService) {
            this.$http = $http;
            this.someResponse = -1;
        }

        public DoSomeThing = () => {
            this.$http.get("someAddress")
                .success((response: number) => {
                    this.someResponse = response;
                }).error(() => {
                    this.someResponse = -2;
                });
        }
    }
}
