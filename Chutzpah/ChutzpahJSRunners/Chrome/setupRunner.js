(async function () {

    require('puppeteer/install');

    process.exit(0);
})().catch(e => {
    console.error(e);
    process.exit(1);
});