import { BlockProperties, ComponentDefinition, Plugin } from 'grapesjs';

export type PluginOptions = {
	/**
	 * The ID used to create the block and component
	 * @default 'countdown'
	 */
	id?: string;
	/**
	 * The label used for the block and the component.
	 * @default 'Countdown'
	 */
	label?: string;
	/**
	 * Object to extend the default block. Pass a falsy value to avoid adding the block.
	 * @example
	 * { label: 'Countdown', category: 'Extra', ... }
	 */
	block?: Partial<BlockProperties>;
	/**
	 * Object to extend the default component properties.
	 * @example
	 * { name: 'Countdown', droppable: false, ... }
	 */
	props?: ComponentDefinition;
	/**
	 * Custom CSS styles for the component. This will replace the default one.
	 * @default ''
	 */
	style?: string;
	/**
	 * Additional CSS styles for the component. These will be appended to the default one.
	 * @default ''
	 */
	styleAdditional?: string;
	/**
	 * Default start time.
	 * @default ''
	 * @example '2018-01-25 00:00'
	 */
	startTime?: string;
	/**
	 * Text to show when the countdown is ended.
	 * @default 'EXPIRED'
	 */
	endText?: string;
	/**
	 * Date input type, eg. `date`, `datetime-local`
	 * @default 'date'
	 */
	dateInputType?: string;
	/**
	 * Days label text used in component.
	 * @default 'days'
	 */
	labelDays?: string;
	/**
	 * Hours label text used in component.
	 * @default 'hours'
	 */
	labelHours?: string;
	/**
	 * Minutes label text used in component.
	 * @default 'minutes'
	 */
	labelMinutes?: string;
	/**
	 * Seconds label text used in component.
	 * @default 'seconds'
	 */
	labelSeconds?: string;
	/**
	 * Countdown component class prefix.
	 * @default 'countdown'
	 */
	classPrefix?: string;
};
declare const plugin: Plugin<PluginOptions>;

export {
	plugin as default,
};

export {};
