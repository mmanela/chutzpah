// This references file checks to see that we resolve references relative to current referenced file
/// <reference path="Calculator.js" />

// This is to make sure we take care of the infinite loop scenario
/// <reference path="references.js" />
/// <reference path="../Test/references.js" />
