/// <reference path="../Web/Scripts/react/react.js"/>
/// <reference path="../Web/Scripts/react/react-dom.js"/>
/// <reference path="../Web/Scripts/react/react-with-addons.js"/>

/// <reference path="./bin/generated/react-components/example.generated.js"/>

QUnit.test("Example test", function (assert) {
    let TestUtils = React.addons.TestUtils;
    let shallowRenderer = TestUtils.createRenderer();

    shallowRenderer.render(<SomeComponent />);
    var component = shallowRenderer.getRenderOutput();
    assert.equal(component.type, "div");
});