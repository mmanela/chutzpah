test("Will work with zero", function(){
	var result = MyNumbers.triple(0);

	equal(result, 0);
});

test("Will triple a number", function(){
	var result = MyNumbers.triple(2);

	equal(result, 6);
});
