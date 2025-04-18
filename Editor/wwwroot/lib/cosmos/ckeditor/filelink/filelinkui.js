/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { icons, Plugin, ButtonView } from 'ckeditor5';
//import ButtonView from '@ckeditor/ckeditor5-ui/src/button/buttonview';
const browseFileIcon = "<svg viewBox='0 0 20 20' xmlns='http://www.w3.org/2000/svg'><path d='M11.627 16.5zm5.873-.196zm0-7.001V8h-13v8.5h4.341c.191.54.457 1.044.785 1.5H2a1.5 1.5 0 0 1-1.5-1.5v-13A1.5 1.5 0 0 1 2 2h4.5a1.5 1.5 0 0 1 1.06.44L9.122 4H16a1.5 1.5 0 0 1 1.5 1.5v1A1.5 1.5 0 0 1 19 8v2.531a6.027 6.027 0 0 0-1.5-1.228zM16 6.5v-1H8.5l-2-2H2v13h1V8a1.5 1.5 0 0 1 1.5-1.5H16z'/><path d='M14.5 19.5a5 5 0 1 1 0-10 5 5 0 0 1 0 10zM15 14v-2h-1v2h-2v1h2v2h1v-2h2v-1h-2z'/></svg>";
export default class FileLinkUI extends Plugin {
	static get pluginName() {
		return 'FileLink';
	}

	init() {
		const editor = this.editor;
		const t = editor.t;
		const model = editor.model;

		// Add the "cosmoswpspagelinkButton" to feature components.
		editor.ui.componentFactory.add( 'fileLink', locale => {
			const view = new ButtonView( locale );

			view.set( {
				label: t( 'Link to file uploaded to this website.' ),
				icon: browseFileIcon,
				tooltip: true
			} );

			// Insert a text into the editor after clicking the button.
			this.listenTo( view, 'execute', () => {
                if (window.parent.openInsertFileLinkModel) {
                    window.parent.openInsertFileLinkModel(editor);
                } else {
                    alert("Executed window.parent.openInsertFileLinkModel()");
                }
			} );

			return view;
		} );
	}
}