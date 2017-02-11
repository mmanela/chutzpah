/// <reference path="../../node_modules/@types/jasmine/index.d.ts" />
System.register(["../App/app.component", "@angular/core/testing", "@angular/platform-browser-dynamic/testing", "@angular/platform-browser"], function (exports_1, context_1) {
    "use strict";
    var __moduleName = context_1 && context_1.id;
    var app_component_1, testing_1, testing_2, platform_browser_1;
    return {
        setters: [
            function (app_component_1_1) {
                app_component_1 = app_component_1_1;
            },
            function (testing_1_1) {
                testing_1 = testing_1_1;
            },
            function (testing_2_1) {
                testing_2 = testing_2_1;
            },
            function (platform_browser_1_1) {
                platform_browser_1 = platform_browser_1_1;
            }
        ],
        execute: function () {/// <reference path="../../node_modules/@types/jasmine/index.d.ts" />
            // The following initializes the test environment for Angular 2. This call is required for Angular 2 dependency injection.
            // That's new in Angular 2 RC5
            testing_1.TestBed.resetTestEnvironment();
            testing_1.TestBed.initTestEnvironment(testing_2.BrowserDynamicTestingModule, testing_2.platformBrowserDynamicTesting());
            describe("AppComponent -> ", function () {
                var de;
                var comp;
                var fixture;
                beforeEach(testing_1.async(function () {
                    testing_1.TestBed.configureTestingModule({
                        declarations: [app_component_1.AppComponent]
                    })
                        .compileComponents();
                }));
                beforeEach(function () {
                    fixture = testing_1.TestBed.createComponent(app_component_1.AppComponent);
                    comp = fixture.componentInstance;
                    de = fixture.debugElement.query(platform_browser_1.By.css('h1'));
                });
                it('should create component', function () { return expect(comp).toBeDefined(); });
                it("Evaluate true condition - 01", function () {
                    expect(1).toBe(1);
                });
            });
        }
    };
});
//# sourceMappingURL=app.component.spec.js.map