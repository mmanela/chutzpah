
  test('testing "quotes" to \'quotes\' for <title> text', function () {
      equal(" this is a quote\" <-- see", " this is a quote\" <-- see");
      equal("this is not ' <script>hello</script>", "this is not ' <script>hello</script>");
  });
