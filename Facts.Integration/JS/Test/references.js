/// <reference path="../Code/code.js" />
/// <reference path="../Code/references.js" />

// This is to make sure we take care of the infinite loop scenario
/// <reference path="references.js" />

window.rightOrder = true;
try {
    var aliasStringLib = stringLib.vowels;
}
catch(e) {
    // If we error referencing a variable from the code.js file then 
    // the files are ordered wrong
    window.rightOrder = false;
}