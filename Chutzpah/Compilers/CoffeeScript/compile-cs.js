
function compilify_cs(code, bare) {
    bare = typeof (bare) != 'undefined' ? bare : true;
    try {
        return CoffeeScript.compile(code, { bare: bare });
    } catch (e) {
        // Without this, we get 'Exception thrown and not caught' as
        // error message instead.
        throw new Error(e.message);
    }
}