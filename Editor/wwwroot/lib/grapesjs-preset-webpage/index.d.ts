import { Editor, Plugin } from 'grapesjs';

export type PluginOptions = {
	/**
	 * Which blocks to add.
	 * @default ['link-block', 'quote', 'text-basic']
	 */
	blocks?: string[];
	/**
	 * Add custom block options, based on block id.
	 * @default (blockId) => ({})
	 * @example (blockId) => blockId === 'quote' ? { attributes: {...} } : {};
	 */
	block?: (blockId: string) => ({});
	/**
	 * Modal import title.
	 * @default 'Import'
	 */
	modalImportTitle?: string;
	/**
	 * Modal import button text.
	 * @default 'Import'
	 */
	modalImportButton?: string;
	/**
	 * Import description inside import modal.
	 * @default ''
	 */
	modalImportLabel?: string;
	/**
	 * Default content to setup on import model open.
	 * Could also be a function with a dynamic content return (must be a string).
	 * @default ''
	 * @example editor => editor.getHtml()
	 */
	modalImportContent?: string | ((editor: Editor) => string);
	/**
	 * Code viewer (eg. CodeMirror) options.
	 * @default {}
	 */
	importViewerOptions?: Record<string, any>;
	/**
	 * Confirm text before clearing the canvas.
	 * @default 'Are you sure you want to clear the canvas?'
	 */
	textCleanCanvas?: string;
	/**
	 * Show the Style Manager on component change.
	 * @default true
	 */
	showStylesOnChange?: boolean;
	/**
	 * Load custom preset theme.
	 * @default true
	 */
	useCustomTheme?: boolean;
};
export type RequiredPluginOptions = Required<PluginOptions>;
declare const plugin: Plugin<PluginOptions>;

export {
	plugin as default,
};

export {};
