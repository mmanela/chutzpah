require.config({
    
    paths: {
        hello: 'base/jquery.hello',
    },

    shim: {
        hello: { deps: ["jquery"] },
    }
});