require.config({
    baseUrl: "../../src",
    paths: {
        hello: 'base/jquery.hello',
    },

    shim: {
        hello: { deps: ["jquery"] },
    }
});