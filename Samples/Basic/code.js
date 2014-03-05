var stringLib = {
    vowels: function (a) {
        count = 0;
        for (var i = 0; i < a.length; i++) {
            if ("aeiou".indexOf(a[i]) > -1) {
                count++;
            }
        }
        return count;
    }
}

var mathLib = {
    add5: function (a) {
        return a + 5;
    },
    mult5: function (a) {
        return a * 5;
    }
}