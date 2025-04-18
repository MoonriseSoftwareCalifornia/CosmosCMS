import { Component, Plugin } from 'grapesjs';
import tuiImageEditor from 'tui-image-editor';

export type ImageEditor = tuiImageEditor.ImageEditor;
export type IOptions = tuiImageEditor.IOptions;
export type PluginOptions = {
	/**
	 * TOAST UI's configurations
	 * https://nhn.github.io/tui.image-editor/latest/ImageEditor
	 */
	config?: IOptions;
	/**
	 * Pass the editor constructor.
	 * By default, the `tui.ImageEditor` will be used.
	 */
	constructor?: any;
	/**
	 * Label for the image editor (used in the modal)
	 * @default 'Image Editor'
	 */
	labelImageEditor?: string;
	/**
	 * Label used on the apply button
	 * @default 'Apply'
	 */
	labelApply?: string;
	/**
	 * Default editor height
	 * @default '650px'
	 */
	height?: string;
	/**
	 * Default editor width
	 * @default '100%'
	 */
	width?: string;
	/**
	 * Id to use to create the image editor command
	 * @default 'tui-image-editor'
	 */
	commandId?: string;
	/**
	 * Icon used in the image component toolbar. Pass an empty string to avoid adding the icon.
	 */
	toolbarIcon?: string;
	/**
	 * Hide the default editor header
	 * @default true
	 */
	hideHeader?: boolean;
	/**
	 * By default, GrapesJS takes the modified image, adds it to the Asset Manager and update the target.
	 * If you need some custom logic you can use this custom 'onApply' function.
	 * @example
	 * onApply: (imageEditor, imageModel) => {
	 *    const dataUrl = imageEditor.toDataURL();
	 *    editor.AssetManager.add({ src: dataUrl }); // Add it to Assets
	 *    imageModel.set('src', dataUrl); // Update the image component
	 * }
	 */
	onApply?: ((imageEditor: ImageEditor, imageModel: Component) => void) | null;
	/**
	 * If no custom `onApply` is passed and this option is `true`, the result image will be added to assets
	 * @default true
	 */
	addToAssets?: boolean;
	/**
	 * If no custom `onApply` is passed, on confirm, the edited image, will be passed to the
	 * AssetManager's uploader and the result (eg. instead of having the dataURL you'll have the URL)
	 * will be passed to the default `onApply` process (update target, etc.)
	 */
	upload?: boolean;
	/**
	 * The apply button (HTMLElement) will be passed as an argument to this function, once created.
	 * This will allow you a higher customization.
	 */
	onApplyButton?: (btn: HTMLElement) => void;
	/**
	 * Scripts to load dynamically in case no TOAST UI editor instance was found
	 */
	script?: string[];
	/**
	 * In case the script is loaded this style will be loaded too
	 */
	style?: string[];
};
declare const plugin: Plugin<PluginOptions>;

export {
	plugin as default,
};

export {};
