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

    var settings = TypeScript.defaultSettings;
    
    if (codeGenTarget === "ES3") {
        settings.codeGenTarget = TypeScript.CodeGenTarget.ES3;
        TypeScript.codeGenTarget = TypeScript.CodeGenTarget.ES3;
    }
    else if (codeGenTarget === "ES5") {
        settings.codeGenTarget = TypeScript.CodeGenTarget.ES5;
        TypeScript.codeGenTarget = TypeScript.CodeGenTarget.ES5;
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

    function createFile(fileName) {
        var buffer = new Buffer();
        convertedFileMap[fileName] = buffer;
        return buffer;
    }

    var errors = new Buffer();
    var logger = new Buffer();
    try {
        var compiler = new TypeScript.TypeScriptCompiler(errors, logger, settings);
        for (var fileName in fileMap) {
            if (fileMap.hasOwnProperty(fileName)) {
                compiler.addUnit(fileMap[fileName], fileName);
            }
        }
        compiler.typeCheck();
        compiler.emit(createFile);
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