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
    this.hasErrors = false;
}

ErrorReporter.prototype = {
    addDiagnostic: function (diagnostic) {
        var message = "";
        var diagnosticInfo = diagnostic.info();
        if (diagnosticInfo.category === 1 /* Error */) {
            this.hasErrors = true;
        }

        if (diagnostic.fileName()) {
            message += diagnostic.fileName() + "(" + (diagnostic.line() + 1) + "," + (diagnostic.character() + 1) + "): ";
        }

        message += diagnostic.message();

        this.errorMessages.push(message);
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

    compilationSettings = TypeScript.ImmutableCompilationSettings.fromCompilationSettings(compilationSettings);
    
    var fileMap = JSON.parse(fileMapStr);
    var convertedFileMap = {};

    var logger = new Buffer();
    var compiler = new TypeScript.TypeScriptCompiler(logger, compilationSettings);

    var errorReporter = new ErrorReporter();
    
    // add the rest
    for (var fileName in fileMap) {
        if (fileMap.hasOwnProperty(fileName)) {
            var fileText = fileMap[fileName];
            var snapshot = TypeScript.ScriptSnapshot.fromString(fileText);
            var referencedFiles = TypeScript.getReferencedFiles(fileName, snapshot);
            compiler.addFile(fileName, snapshot, "None", 0, true, referencedFiles);
        }
    }

    for (var it = compiler.compile() ; it.moveNext() ;) {
        var result = it.current();

        result.diagnostics.forEach(function (d) {
            return errorReporter.addDiagnostic(d);
        });

        result.outputFiles.forEach(function (outputFile) {
            convertedFileMap[outputFile.name] = outputFile.text;
        });
    }


    if (errorReporter.hasErrors) {
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