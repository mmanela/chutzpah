test("Will raise a number by another", function(){
	var result = MyMath.pow(2, 4);

	equal(result, 16);
});

test("Will raise a number by 1", function(){
	var result = MyMath.pow(2, 1);

	equal(result, 2);
});