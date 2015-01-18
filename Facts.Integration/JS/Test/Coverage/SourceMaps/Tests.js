test("my test", function () {
    // Just make some method calls - we're less interested in the test outcome
    // as the coverage figures
    var staticAdd = SourceMaps.Library.MathUtil.StaticAdd(1, 2);
    var staticIsEven = SourceMaps.Library.MathUtil.StaticIsEven(2);

    var mathUtil = new SourceMaps.Library.MathUtil();

    var add = mathUtil.Add(1, 2);
    var isEven = mathUtil.IsEven(2);

    expect(0);
});
