/// <chutzpah_reference path="../code/code.js" />

  test("will load reference using chutzpah_reference syntax", function () {
      var res = mathLib.add5(10);

      equal(res, 15);
  });
