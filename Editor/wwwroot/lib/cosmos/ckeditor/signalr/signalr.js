/*
* @license MIT
* @copyright Copyright (c) 2022-2023 Moonrise Software LLC.
*/

import { Plugin } from 'ckeditor5';
import SignalRUI from './signalrui.js';
//import SignalREditing from "./signalrediting";

export default class SignalR extends Plugin {
    static get requires() {
        return [SignalRUI];
    }
}