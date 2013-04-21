window.ddescribeIitSupport = (function (instance) {
    if (instance) return instance; // prevent double init

    var exclusive = {};

    function isExclusive(spec) {
        if (exclusive.specs) {
            // There are exclusive specs (iit), so we run only those.
            return exclusive.specs[spec.getFullName()];
        }

        // See if the spec belongs to an exclusive suite (ddescribe).
        for (var suite = spec.suite; exclusive.suites && suite; suite = suite.parentSuite) {
            if (exclusive.suites[suite.getFullName()])
                return true;
        }
        return false;
    }

    function patchGlobal() {
        // Add ddescribe and iit to window. If Jasmine has them and is included later, they will be overwritten.
        // Jasmine 2.0 removes globals, so this code will probably be affected at that point.
        window.ddescribe = function () {
            var jasmineEnv = jasmine.getEnv();
            var suite = jasmine.Env.prototype.describe.apply(jasmineEnv, Array.prototype.slice.call(arguments, 0));
            (exclusive.suites || (exclusive.suites = {}))[suite.getFullName()] = true;
            return suite;
        };
        window.iit = function () {
            var jasmineEnv = jasmine.getEnv();
            var spec = jasmine.Env.prototype.it.apply(jasmineEnv, Array.prototype.slice.call(arguments, 0));
            (exclusive.specs || (exclusive.specs = {}))[spec.getFullName()] = true;
            return spec;
        };
    }

    function patchJasmine(jasmineEnv) {
        if (jasmineEnv.specFilter_) return; // already patched
        var specFilter = function (spec) {
            var run = true;
            if (exclusive.suites || exclusive.specs) {
                run = isExclusive(spec);
            }
            return run && jasmineEnv.specFilter_.call(jasmineEnv, spec);
        };
        jasmineEnv.specFilter_ = jasmineEnv.specFilter;
        jasmineEnv.specFilter = specFilter;
    }

    if (!window.ddescribe) {
        patchGlobal();
    }

    return {
        patch: patchJasmine
    };
})(window.ddescribeIitSupport);

