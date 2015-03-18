test("Will add two numbers", function(){
	var result = MyMath.add(1,2);

	equal(result, 3);
});

test("Will add zero to a number", function(){
	var result = MyMath.add(0,2);

	equal(result, 2);
});