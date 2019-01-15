# ![](https://raw.githubusercontent.com/mmanela/chutzpah/master/doc/images/chetTimesSmall.png) Chutzpah - A JavaScript Test Runner
Pronunciation: [hutz·pah](http://www.thefreedictionary.com/chutzpah)

[![Build Status](https://mmanela.visualstudio.com/Chutzpah/_apis/build/status/ChutzpahBuild)](https://mmanela.visualstudio.com/Chutzpah/_build/latest?definitionId=1)
[![Chutzpah NuGet version](https://img.shields.io/nuget/v/Chutzpah.svg)](https://www.nuget.org/packages/Chutzpah/)

Chutzpah is an open source JavaScript test runner which enables you to run unit tests using QUnit, Jasmine, Mocha, CoffeeScript and TypeScript.

_For comments, praise, complaints you can reach me on twitter at [@mmanela](http://twitter.com/mmanela)_.

Chutzpah supports the [QUnit](http://docs.jquery.com/QUnit), [Jasmine](https://jasmine.github.io/) and [Mocha](http://mochajs.org/) testing frameworks. 
Chutzpah uses the [PhantomJS](http://www.phantomjs.org/) headless browser to run your tests.


## Get Chutzpah

* Command Line Runner [nuget](https://www.nuget.org/packages/Chutzpah) or [chocolatey](http://chocolatey.org/packages/chutzpah)
* [Visual Studio Test Adapter for Visual Studio 2015+](http://visualstudiogallery.msdn.microsoft.com/f8741f04-bae4-4900-81c7-7c9bfb9ed1fe)
* [Visual Studio Test Runner for 2015+](http://visualstudiogallery.msdn.microsoft.com/71a4e9bd-f660-448f-bd92-f5a65d39b7f0)


## Test Framework Versions
- Qunit 2.6.2
- Jasmine 2.9
- Mocha 3.2.0

## Recent News

* **[Chutzpah 4.3 - Web Server Mode](http://matthewmanela.com/blog/chutzpah-4-3-0-web-server-mode/)**
* [Chutzpah 4.0 – Batching, Inheritance and more](http://matthewmanela.com/blog/chutzpah-4-0-batching-inheritance-and-more/)*
* [Chutzpah 3.3](http://matthewmanela.com/blog/chutzpah-3-3-0/)
* [Chutzpah 3.2 – A smarter approach to compilation](http://matthewmanela.com/blog/chutzpah-3-2-a-smarter-approach-to-compilation/)


## Quick Links
* [Full Documentation](https://github.com/mmanela/chutzpah/wiki)
* [How do I use Chutzpah?](https://github.com/mmanela/chutzpah/wiki/Running-JavaScript-tests-with-Chutzpah)
* [How can I use it with RequireJS?](https://github.com/mmanela/chutzpah/wiki/Running-RequireJS-unit-tests)
* [How can I use it with TypeScript?](https://github.com/mmanela/chutzpah/wiki/Running-Unit-Tests-written-in-TypeScript)
* [How can I use it with CoffeeScript?](https://github.com/mmanela/chutzpah/wiki/Running-Unit-Tests-written-in-CoffeeScript)
* [How to build the code?](https://github.com/mmanela/chutzpah/wiki/building-and-running-the-code)
* [How can I contribute to this project?](https://github.com/mmanela/chutzpah/wiki/contributing-to-chutzpah)

## Building the code
1. Clone the repo
2. One time run .\build.bat install - This will install all dependencies
3. Run .\build - This will build all the code and run tests.
4. Open solution Chutzpah.NoVS.sln for normal changes

## Features

##### Runs JavaScript unit tests from the command line
  
 ![](https://raw.githubusercontent.com/mmanela/chutzpah/master/doc/images/commandLine.png)



##### Run JavaScript unit tests from inside of Visual Studio

* Right click menu to run tests
  
 ![](https://raw.githubusercontent.com/mmanela/chutzpah/master/doc/images/contextmenu_debugger.png)


* Shows test results in Error window
  
 ![](https://raw.githubusercontent.com/mmanela/chutzpah/master/doc/images/errorWindow.png)


* Shows test results in Ouput window
  
![](https://raw.githubusercontent.com/mmanela/chutzpah/master/doc/images/outputWindow.png)


* Integrates into VS 2012 Unit Test Explorer
  
 ![](https://raw.githubusercontent.com/mmanela/chutzpah/master/doc/images/UnitTestExplorer.png)
