

function spinWait(millis) {
    var date = new Date();
    var curDate;
    do { curDate = new Date(); }
    while (curDate - date < millis);
}

test("Timeout test", function () {
    spinWait(1000);
    ok(true, "this test is just long enough");
});
