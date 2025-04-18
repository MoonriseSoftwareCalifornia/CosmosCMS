const script = function () {
    var element = this;
    ccms___setupImageWidget(element);
};

const cosmosImageWidgetPlugin = (editor) => {
    // Register a new component type
    editor.Components.addType('ImageContainer', {
        // Make the editor understand how to recognize the component from parsed HTML
        isComponent: el => el.classList?.contains('ccms-img-widget'),
        // Provide the default properties of the component (more about it in the next section).
        model: {
            defaults: {
                script,
                attributes: {
                    'data-ccms-new': "true",
                    'data-editor-config': 'image-widget',
                },
                droppable: false,
            }
        }
    });
    editor.Blocks.add('ImageContainer', {
        label: 'Image Widget',
        category: 'Cosmos CMS',
        media: '<svg viewBox="0 0 24 24"><path fill="currentColor" d="M21,3H3C2,3 1,4 1,5V19A2,2 0 0,0 3,21H21C22,21 23,20 23,19V5C23,4 22,3 21,3M5,17L8.5,12.5L11,15.5L14.5,11L19,17H5Z"></path></svg>',
        content: '<div class="ccms-img-widget" style="min-height: 60px;width: 100%;"></div>'
    });
};
