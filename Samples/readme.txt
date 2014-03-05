Chutzpah Samples 

* RequireJS
  - This folder contains samples for how to setup requirejs tests that use QUnit, Mocha, Jasmine and TypeScript. In addition it contains sample of using RequireJS with custom baseUrl and test harness locations.

* Basic
 - This folder contains basic examples of running tests in QUnit, Jasmine, Mocha (with each support interface) and written in JavsScript, CoffeeScript and TypeScript
 
 
 
 Examples
 
 * Running a test file
    chutzpah.console.exe Basic\QUnit\basic-qunit.js
 
 
 * Running with code coverage
   chutzpah.console.exe Basic\Mocha\basic-mocha-bdd.ts /coverage
   

 * Opening a test file in the browser
    chutzpah.console.exe Basic\Jasmine\basic-jasmine.js /openinbrowser
    
    
 * Running multiple test files
    chutzpah.console.exe /path RequireJS\QUnit\tests\base\base.qunit.test.js /path RequireJS\QUnit\tests\ui\ui.qunit.test.js