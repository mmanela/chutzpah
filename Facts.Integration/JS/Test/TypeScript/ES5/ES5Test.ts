
module es5Conversion {


    export class Options {

        static get debug(): boolean { return true; }

    }

   
}

declare function test(message:string, func: () => void) : void;
declare function ok(expression: boolean, message?: string) : void;


test("ES5 should work when set to", () =>
{

	ok(es5Conversion.Options.debug);

});


