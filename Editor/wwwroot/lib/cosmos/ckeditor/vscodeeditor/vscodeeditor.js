/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { Plugin } from 'ckeditor5';
import VSCodeEditorEditing from './vscodeeditorediting.js';
import VSCodeEditorUI from './vscodeeditorui.js';

export default class VSCodeEditor extends Plugin {
    static get requires() {
        return [VSCodeEditorEditing, VSCodeEditorUI];
    }
}