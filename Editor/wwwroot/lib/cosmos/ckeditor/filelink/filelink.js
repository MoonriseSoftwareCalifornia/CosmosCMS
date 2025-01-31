/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { Plugin } from 'ckeditor5';
import FileLinkUI from './filelinkui.js';
import FileLinkEditing from './filelinkediting.js';

export default class FileLink extends Plugin {
    static get requires() {
        return [FileLinkUI, FileLinkEditing];
    }
}