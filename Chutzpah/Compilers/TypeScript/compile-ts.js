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

function compilify_ts(fileMapStr) {
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
    var compiler = new TypeScript.TypeScriptCompiler(errors, logger);
    for (var fileName in fileMap) {
        if (fileMap.hasOwnProperty(fileName)) {
            compiler.addUnit(fileMap[fileName], fileName);
        }
    }
    compiler.typeCheck();
    compiler.emit(createFile);

    var convertedMapWithOriginalFileNames = {};
    for (var file in convertedFileMap) {
        convertedMapWithOriginalFileNames[changeExtension(file, "ts")] = convertedFileMap[file];
    }

    return JSON.stringify(convertedMapWithOriginalFileNames);
}