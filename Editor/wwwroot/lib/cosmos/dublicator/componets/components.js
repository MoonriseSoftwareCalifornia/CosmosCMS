/**
 * Generate a random GUID.
 * @returns a GUID.
 */
const generateGUID = function () {
    function s4() {
        return Math.floor((1 + Math.random()) * 0x10000)
            .toString(16)
            .substring(1);
    }
    return s4() + s4() + '-' + s4() + '-' + s4() + '-' + s4() + '-' + s4() + s4() + s4();
}

/**
 * Card element.
 */
const cardElement = {
    /**
    * Get the card element.
    * @returns a card element.
    * @type {HTMLElement}
    * @readonly
    * @memberof cardElement
    */
    get: function () {
        const card = document.createElement('div');
        card.classList.add('card');
        const body = document.createElement('div');
        body.classList.add('card-body');
        body.appendChild(imageWidget.get());
        body.appendChild(ckEditorArea.get());
        card.appendChild(body);
        return card;
    }
}

/**
 * Image widget.
 */
const imageWidget = {
    /**
     * Get the image widget.
     * @returns an image widget.
     * @type {HTMLElement}
     * @readonly
     * @memberof imageWidget
     */
    get: function () {
        const div = document.createElement('div');
        div.classList.add('img-block');
        div.classList.add('ck-content');
        div.addAttributes('data-ccms-ceid', generateGUID());
        div.addAttributes('data-editor-config', 'image-widget');
        div.style.position = 'relative';
        return div;
    }
}

/**
 * CKEditor area.
 */
const ckEditorArea = {
    /**
     * Get the CKEditor area.
     * @returns a CKEditor area.
     * @type {HTMLElement}
     * @readonly
     * @memberof ckEditorArea
     */
    get: function () {
        const div = document.createElement('div');
        div.classList.add('img-block');
        div.classList.add('ck-content');
        div.addAttributes('data-ccms-ceid', generateGUID());
        div.addAttributes('data-editor-config', 'ckeditor');
        return div;
    }
}