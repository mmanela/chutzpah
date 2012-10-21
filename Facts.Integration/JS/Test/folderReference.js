/// <reference path="../Code/SubFolder" />

/*
    This test depends on a function getName which is located in ../Code/SubFolder/name.js.  
    The folder containing that file is referenced above. Chutzpah will import all files from that folder.
*/

 module("Folder Referencing Testing");
 test("Test importing files from folder", function () {
     var name = "Chutzpah";
     
     var result = getName();

     equal(result, name);
 });