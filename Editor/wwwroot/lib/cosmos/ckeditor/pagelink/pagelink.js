/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { Plugin } from 'ckeditor5';
import PageLinkEditing from './pagelinkediting.js';
import PageLinkUI from './pagelinkui.js';

export default class PageLink extends Plugin {
    static get requires() {
        return [PageLinkEditing, PageLinkUI];
    }
}