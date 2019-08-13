declare class StringBase {
    content: string;
    vowels: string;
    constructor(content: string);
}
declare class StringPlus extends StringBase {
    content: string;
    vowels: string;
    constructor(content: string);
    countVowels(): number;
}
declare var mathLib: {
    add5: (a: number) => number;
    mult5: (a: number) => number;
};
