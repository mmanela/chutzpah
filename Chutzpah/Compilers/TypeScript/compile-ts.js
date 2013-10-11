function Buffer() {
    this.buffer = "";
}

Buffer.prototype = {
    Write: function (s) {
        this.buffer += s;
    },
    WriteLine: function (s) {
        this.buffer += s + "\n";
    },
    toString: function () {
        return this.buffer;
    },
    log: function (x) {

    },
    Close: function () {
    },
    toJSON: function () {
        return this.buffer;
    }
};

function ErrorReporter() {
    this.errorMessages = [];
    this.currentFile = "";
}

ErrorReporter.prototype = {
    addDiagnostic: function (diagnostic) {
        this.errorMessages.push(this.currentFile + ": " + diagnostic.message());
    },
    setCurrentFile: function (fileName) {
        this.currentFile = fileName;
    },
    doThrow: function () {
        throw new Error(this.errorMessages.join("\r\n"));
    }
};

function compilify_ts(fileMapStr, codeGenTarget, moduleKind) {
    var compilationSettings = new TypeScript.CompilationSettings();

    if (codeGenTarget === "ES3") {
        compilationSettings.codeGenTarget = TypeScript.LanguageVersion.EcmaScript3;
    } else if (codeGenTarget === "ES5") {
        compilationSettings.codeGenTarget = TypeScript.LanguageVersion.EcmaScript5;
    }
    
    if (moduleKind === "CommonJS") {
        compilationSettings.moduleGenTarget = 1;
    } else if (moduleKind === "AMD") {
        compilationSettings.moduleGenTarget = 2;
    }

    var fileMap = JSON.parse(fileMapStr);
    var convertedFileMap = {};

    var logger = new Buffer();
    var compiler = new TypeScript.TypeScriptCompiler(logger, compilationSettings);

    var anySyntacticErrors = false;
    var anySemanticErrors = false;

    var errorReporter = new ErrorReporter();
    
    // add the rest
    for (var fileName in fileMap) {
        if (fileMap.hasOwnProperty(fileName)) {
            var fileText = fileMap[fileName];
            var snapshot = TypeScript.ScriptSnapshot.fromString(fileText);
            var referencedFiles = TypeScript.getReferencedFiles(fileName, snapshot);
            compiler.addSourceUnit(fileName, snapshot, "None", 0, true, referencedFiles);

            var syntacticDiagnostics = compiler.getSyntacticDiagnostics(fileName);
            errorReporter.setCurrentFile(fileName);
            compiler.reportDiagnostics(syntacticDiagnostics, errorReporter);

            if (syntacticDiagnostics.length > 0) {
                anySyntacticErrors = true;
            }
        }
    }

    if (anySyntacticErrors) {
        errorReporter.doThrow();
    }

    compiler.pullTypeCheck();
    var fileNames = compiler.fileNameToDocument.getAllKeys();
    for (var i = 0, n = fileNames.length; i < n; i++) {
        var fileName = fileNames[i];
        var semanticDiagnostics = compiler.getSemanticDiagnostics(fileName);
        if (semanticDiagnostics.length > 0) {
            anySemanticErrors = true;
            errorReporter.setCurrentFile(fileName);
            compiler.reportDiagnostics(semanticDiagnostics, errorReporter);
        }
    }

    var emitterIOHost = {
        writeFile: function(fileName, contents, writeByteOrderMark) {
            var buffer = new Buffer();
            buffer.Write(contents);
            convertedFileMap[fileName] = buffer;
        },
        fileExists: function(path) {
            return false;
        },
        directoryExists: function(path) {
            return false;
        },
        resolvePath: function(path) {
            return path;
        }
    };

    var emitDiagnostics = compiler.emitAll(emitterIOHost);
    errorReporter.setCurrentFile("");
    compiler.reportDiagnostics(emitDiagnostics, errorReporter);
    if (emitDiagnostics.length > 0 || anySemanticErrors) {
        errorReporter.doThrow();
    }

    var emitDeclarationsDiagnostics = compiler.emitAllDeclarations();
    errorReporter.setCurrentFile("");
    compiler.reportDiagnostics(emitDeclarationsDiagnostics, errorReporter);
    if (emitDeclarationsDiagnostics.length > 0) {
        errorReporter.doThrow();
    }

    function changeExtension(fname, ext) {
        var splitFname = fname.split(".");
        splitFname.pop();
        var baseName = splitFname.join(".");
        var outFname = baseName + "." + ext;
        return outFname;
    }

    var convertedMapWithOriginalFileNames = {};
    for (var file in convertedFileMap) {
        convertedMapWithOriginalFileNames[changeExtension(file, "ts")] = convertedFileMap[file];
    }

    return JSON.stringify(convertedMapWithOriginalFileNames);
}