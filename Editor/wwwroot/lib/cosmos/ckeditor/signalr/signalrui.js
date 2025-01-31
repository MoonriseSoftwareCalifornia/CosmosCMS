/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { Plugin } from 'ckeditor5';

export default class SignalRUI extends Plugin {
	static get pluginName() {
		return 'SignalR';
	}

    init() {
		const editor = this.editor;

        editor.editing.view.document.on('change:isFocused', (evt, name, value) => {
            if (editor.editing.view.document.isFocused) {
                if (parent.cosmosSignalOthers) {
                    parent.cosmosSignalOthers(editor, "focus");
                } else {
                    console.log("focus");
                }
            } else {
                if (parent.cosmosSignalOthers) {
                    parent.cosmosSignalOthers(editor, "blur");
                } else {
                    console.log("blur");
                }
            }
        });
        editor.editing.view.document.on("keydown", (evt, name, value) => {
            if (parent.cosmosSignalOthers) {
                parent.cosmosSignalOthers(editor, "keydown");
            } else {
                console.log("keydown");
            }
        });
        editor.editing.view.document.on("mousedown", (evt, name, value) => {
            if (parent.cosmosSignalOthers) {
                parent.cosmosSignalOthers(editor, "mousedown");
            } else {
                console.log("mousedown");
            }
        });
    }
}