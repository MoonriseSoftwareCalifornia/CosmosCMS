/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { Plugin, ButtonView } from 'ckeditor5';
const addImageIcon = "<svg viewBox='0 0 20 20' xmlns='http://www.w3.org/2000/svg'><path d='M6.91 10.54c.26-.23.64-.21.88.03l3.36 3.14 2.23-2.06a.64.64 0 0 1 .87 0l2.52 2.97V4.5H3.2v10.12l3.71-4.08zm10.27-7.51c.6 0 1.09.47 1.09 1.05v11.84c0 .59-.49 1.06-1.09 1.06H2.79c-.6 0-1.09-.47-1.09-1.06V4.08c0-.58.49-1.05 1.1-1.05h14.38zm-5.22 5.56a1.96 1.96 0 1 1 3.4-1.96 1.96 1.96 0 0 1-3.4 1.96z'/><path style='fill:white;' stroke='#3D25BE' d='M14.5 19.5a5 5 0 1 1 0-10 5 5 0 0 1 0 10zM15 14v-2h-1v2h-2v1h2v2h1v-2h2v-1h-2z'/></svg>";

export default class InsertImageUI extends Plugin {
	static get pluginName() {
		return 'InsertImage';
	}

	init() {
		const editor = this.editor;
		const t = editor.t;
		const model = editor.model;

		// Add the "cosmoswpspagelinkButton" to feature components.
		editor.ui.componentFactory.add( 'insertImage', locale => {
			const view = new ButtonView( locale );

			view.set( {
				label: t( 'Insert image uploaded to this website.' ),
				icon: addImageIcon,
				tooltip: true
			} );

			// Insert a text into the editor after clicking the button.
			this.listenTo( view, 'execute', () => {
                if (window.parent.openInsertImageModel) {
                    window.parent.openInsertImageModel(editor);
                } else {
                    alert("Executed openInsertImageModel()");
                }
			} );

			return view;
		});
	}
}