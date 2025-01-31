/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { Plugin, ButtonView } from 'ckeditor5';
const addPageLinkIcon = "<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='currentColor' class='bi bi-filetype-html' viewBox='0 0 16 16'><path fill-rule='evenodd' d='M14 4.5V11h-1V4.5h-2A1.5 1.5 0 0 1 9.5 3V1H4a1 1 0 0 0-1 1v9H2V2a2 2 0 0 1 2-2h5.5L14 4.5Zm-9.736 7.35v3.999h-.791v-1.714H1.79v1.714H1V11.85h.791v1.626h1.682V11.85h.79Zm2.251.662v3.337h-.794v-3.337H4.588v-.662h3.064v.662H6.515Zm2.176 3.337v-2.66h.038l.952 2.159h.516l.946-2.16h.038v2.661h.715V11.85h-.8l-1.14 2.596H9.93L8.79 11.85h-.805v3.999h.706Zm4.71-.674h1.696v.674H12.61V11.85h.79v3.325Z'/>  <path d='M4.715 6.542 3.343 7.914a3 3 0 1 0 4.243 4.243l1.828-1.829A3 3 0 0 0 8.586 5.5L8 6.086a1.002 1.002 0 0 0-.154.199 2 2 0 0 1 .861 3.337L6.88 11.45a2 2 0 1 1-2.83-2.83l.793-.792a4.018 4.018 0 0 1-.128-1.287z'/><path d='M6.586 4.672A3 3 0 0 0 7.414 9.5l.775-.776a2 2 0 0 1-.896-3.346L9.12 3.55a2 2 0 1 1 2.83 2.83l-.793.792c.112.42.155.855.128 1.287l1.372-1.372a3 3 0 1 0-4.243-4.243L6.586 4.672z'/></svg>";

export default class PageLinkUI extends Plugin {
    init() {
        const editor = this.editor;
        const t = editor.t;

        this.editor.ui.componentFactory.add('pageLink', locale => {
            const button = new ButtonView(locale);

            button.set({
                label: t('Insert a link to a page on this website.'),
                icon: addPageLinkIcon,
                tooltip: true
            });

            // Insert a text into the editor after clicking the button.
            this.listenTo(button, 'execute', () => {
                if (window.parent.openPickPageModal) {
                    window.parent.openPickPageModal(this.editor);
                } else {
                    alert("Executed openPickPageModal()");
                }
            });

            return button;
        });
    }
}