import { Plugin } from 'grapesjs';
import { PluginOptions as StyleGradientOptions } from 'grapesjs-style-gradient';

export interface PluginOptions {
	/**
	 * Options for the `grapesjs-style-gradient` plugin.
	 * @default {}
	 */
	styleGradientOpts?: StyleGradientOptions;
	/**
	 * Extend single style property definition of the plugin.
	 * You can, for example, change the default gradient color.
	 */
	propExtender?: (prop: any) => any;
}
export declare enum BackgroundType {
	Image = "image",
	Color = "color",
	Grad = "grad"
}
declare const plugin: Plugin<PluginOptions>;

export {
	plugin as default,
};

export {};
