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

function compilify_ts(fileMapStr, codeGenTarget) {
    
    var settings = new TypeScript.CompilationSettings();

    if (codeGenTarget === "ES3") {
        settings.codeGenTarget = TypeScript.LanguageVersion.EcmaScript3;
    }
    else if (codeGenTarget === "ES5") {
        settings.codeGenTarget = TypeScript.LanguageVersion.EcmaScript5;
    }

    var fileMap = JSON.parse(fileMapStr);
    var convertedFileMap = {};

    function changeExtension(fname, ext) {
        var splitFname = fname.split(".");
        splitFname.pop();
        var baseName = splitFname.join(".");
        var outFname = baseName + "." + ext;
        return outFname;
    }

    try {
        var compiler = new TypeScript.TypeScriptCompiler(new TypeScript.NullLogger(), settings);
        for (var fileName in fileMap) {
            if (fileMap.hasOwnProperty(fileName)) {
                var snapshot = TypeScript.ScriptSnapshot.fromString(fileMap[fileName]);
                compiler.addSourceUnit(fileName, snapshot, "None", 0, true);
            }
        }

        // check for errors
        var allErrors = "";
        for (var fileName in fileMap) {
            if (fileMap.hasOwnProperty(fileName)) {
                var syntacticDiagnostics = compiler.getSyntacticDiagnostics(fileName);
                for (var diag in syntacticDiagnostics) {
                    allErrors += fileName + ": " + syntacticDiagnostics[diag].message() + "\n";
                }
            }
        }
        if (allErrors) {
            throw new Error(allErrors);
        }

        compiler.pullTypeCheck();

        compiler.emitAll({
            writeFile: function (fileName, contents, writeByteOrderMark) {
                var buffer = new Buffer();
                buffer.Write(contents);
                convertedFileMap[fileName] = buffer;
            },
            fileExists: function (path) {
                return false;
            },
            directoryExists: function (path) {
                return false;
            },
            resolvePath: function (path) {
                return path;
            }
        });
    } catch (e) {
        // Without this, we get 'Exception thrown and not caught' as
        // error message instead.
        throw new Error(e.message);
    }

    var convertedMapWithOriginalFileNames = {};
    for (var file in convertedFileMap) {
        convertedMapWithOriginalFileNames[changeExtension(file, "ts")] = convertedFileMap[file];
    }

    return JSON.stringify(convertedMapWithOriginalFileNames);
}