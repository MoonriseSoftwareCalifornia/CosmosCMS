import { BlockProperties, Plugin } from 'grapesjs';

export type PluginOptions = {
	/**
	 * Which blocks to add.
	 * @default ['form', 'input', 'textarea', 'select', 'button', 'label', 'checkbox', 'radio']
	 */
	blocks?: string[];
	/**
	 * Category name for blocks.
	 * @default 'Forms'
	 */
	category?: BlockProperties["category"];
	/**
	 * Add custom block options, based on block id.
	 * @default (blockId) => ({})
	 * @example (blockId) => blockId === 'input' ? { attributes: {...} } : {};
	 */
	block?: (blockId: string) => ({});
};
declare const plugin: Plugin<PluginOptions>;

export {
	plugin as default,
};

export {};
