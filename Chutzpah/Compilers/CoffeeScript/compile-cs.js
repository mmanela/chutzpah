
function compilify_cs(code, bare) {
    bare = typeof(bare) != 'undefined' ? bare : true;
    return CoffeeScript.compile(code, { bare: bare });
}