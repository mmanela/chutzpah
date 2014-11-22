function CoverageTarget_Method1() {
    return "Method1";
}
function CoverageTarget_Method2() {
    if (false) {
        return "Impossible to cover";
    }
    else {
        return "Method2";
    }
}