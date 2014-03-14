declare class StringBase {
    public content: string;
    public vowels: string;
    constructor(content: string);
}
declare class StringPlus extends StringBase {
    public content: string;
    public vowels: string;
    constructor(content: string);
    public countVowels(): number;
}
declare var mathLib: {
    add5: (a: number) => number;
    mult5: (a: number) => number;
};
