const sessionStoragePlugin = (editor) => {
    editor.Storage.add('cosmos', {
        async load(options = {}) {
            const response = await fetch(cosmos__designerDataEndpoint + "/" + cosmos__id);
            const data = await response.json();
            return data;
        },
        async store(data, options = {}) {
            showSaving(); // Show saving message.
            const pagesHtml = editor.Pages.getAll().map((page) => {
                const component = page.getMainComponent();
                $("#HtmlContent").val(encryptData(editor.getHtml({ component })));
                $("#CssContent").val(encryptData(editor.getCss({ component })));
                cosmos__designerPostData();
            });
        }
    });
};