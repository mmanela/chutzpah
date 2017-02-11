/// <reference path="../../node_modules/@types/jasmine/index.d.ts" />
     

import { AppComponent } from '../App/app.component';
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { BrowserDynamicTestingModule, platformBrowserDynamicTesting } from '@angular/platform-browser-dynamic/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';
 
          
// The following initializes the test environment for Angular 2. This call is required for Angular 2 dependency injection.
// That's new in Angular 2 RC5
TestBed.resetTestEnvironment();
TestBed.initTestEnvironment(BrowserDynamicTestingModule, platformBrowserDynamicTesting());

describe("AppComponent -> ", () => {
        
    let de: DebugElement; 
    let comp: AppComponent; 
    let fixture: ComponentFixture<AppComponent>;
       
    beforeEach(async(() => {
        TestBed.configureTestingModule({
            declarations: [AppComponent]
        })
            .compileComponents();
    }));      

    beforeEach(() => {  
        fixture = TestBed.createComponent(AppComponent);
        comp = fixture.componentInstance;
        de = fixture.debugElement.query(By.css('h1'));
    });

    it('should create component', () => expect(comp).toBeDefined());

    it("Evaluate true condition - 01", () => {
        expect(1).toBe(1);
    });                 
}); 