/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { Plugin } from 'ckeditor5';
import InsertImageUI from './insertimageui.js';
import InsertImageEditing from './insertimageediting.js';

export default class InsertImage extends Plugin {
    static get requires() {
        return [InsertImageUI, InsertImageEditing];
    }
}