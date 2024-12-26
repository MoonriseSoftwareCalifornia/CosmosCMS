/*!
 * GrapesJS build for Cosmos
 * https://cosmos.moonrise.net
 *
 * Copyright GrapesJS, Moonrise Software, LLC and other contributors.
 * Released under the MIT license
 *
 * Date: 2024-12-12
 */

function ccms___generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0,
            v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

async function cosmos__designerLoadAssets(path, exclude) {
    cosmos__GetImageAssets(path, exclude)
        .then(json => {
            editor.AssetManager.add(json);
            editor.AssetManager.render();
        });
}

async function cosmos__GetImageAssets(path, exclude) {
    const url = "/FileManager/GetImageAssets?path=" + path + "&exclude=" + exclude;
    const response = await fetch(url, {
        method: 'GET'
    });
    return await response.json();
}

const sessionStoragePlugin = (editor) => {
    // As sessionStorage is not an asynchronous API,
    // the `async` keyword could be skipped
    editor.Storage.add('cosmos', {
        async load(options = {}) {
            const response = await fetch(cosmos__designerDataEndpoint + "/" + cosmos__id);
            const data = await response.json();
            return data;
        },
        async store(data, options = {}) {
            showSaving(); // Show saving message.
            const pagesHtml = editor.Pages.getAll().map((page) => {
                const component = page.getMainComponent();
                $("#HtmlContent").val(editor.getHtml({ component }));
                $("#CssContent").val(editor.getCss({ component }));
                cosmos__designerPostData();
            });
        }
    });
};

cosmos_plugin_array.push(sessionStoragePlugin);

const editor = grapesjs.init({
    // Indicate where to init the editor. You can also pass an HTMLElement
    container: '#gjs',
    // Get the content for the canvas directly from the element
    // As an alternative we could use: `components: '<h1>Hello World Component!</h1>,
    fromElement: true,
    showOffsets: true,
    // Size of the editor
    height: "100%",
    width: 'auto',
    canvas: {
        styles: grapesjs__canvas__styles,
        scripts: grapesjs__canvas__scripts
    },
    assetManager: {
        upload: cosmos__designerUploadEndpoint,
        embedAsBase64: false,
        assets: grapesjs__assets,
    },
    selectorManager: {
        selectorManager: { componentFirst: true },
    },
    styleManager: {
        sectors: [{
            name: 'General',
            properties: [
                {
                    extend: 'float',
                    type: 'radio',
                    default: 'none',
                    options: [
                        { value: 'none', className: 'fa fa-times' },
                        { value: 'left', className: 'fa fa-align-left' },
                        { value: 'right', className: 'fa fa-align-right' }
                    ],
                },
                'display',
                { extend: 'position', type: 'select' },
                'top',
                'right',
                'left',
                'bottom',
            ],
        }, {
            name: 'Dimension',
            open: false,
            properties: [
                'width',
                {
                    id: 'flex-width',
                    type: 'integer',
                    name: 'Width',
                    units: ['px', '%'],
                    property: 'flex-basis',
                    toRequire: 1,
                },
                'height',
                'max-width',
                'min-height',
                'margin',
                'padding'
            ],
        }, {
            name: 'Typography',
            open: false,
            properties: [
                'font-family',
                'font-size',
                'font-weight',
                'letter-spacing',
                'color',
                'line-height',
                {
                    extend: 'text-align',
                    options: [
                        { id: 'left', label: 'Left', className: 'fa fa-align-left' },
                        { id: 'center', label: 'Center', className: 'fa fa-align-center' },
                        { id: 'right', label: 'Right', className: 'fa fa-align-right' },
                        { id: 'justify', label: 'Justify', className: 'fa fa-align-justify' }
                    ],
                },
                {
                    property: 'text-decoration',
                    type: 'radio',
                    default: 'none',
                    options: [
                        { id: 'none', label: 'None', className: 'fa fa-times' },
                        { id: 'underline', label: 'underline', className: 'fa fa-underline' },
                        { id: 'line-through', label: 'Line-through', className: 'fa fa-strikethrough' }
                    ],
                },
                'text-shadow'
            ],
        }, {
            name: 'Decorations',
            open: false,
            properties: [
                'opacity',
                'border-radius',
                'border',
                'box-shadow',
                'background', // { id: 'background-bg', property: 'background', type: 'bg' }
            ],
        }, {
            name: 'Extra',
            open: false,
            buildProps: [
                'transition',
                'perspective',
                'transform'
            ],
        }, {
            name: 'Flex',
            open: false,
            properties: [{
                name: 'Flex Container',
                property: 'display',
                type: 'select',
                defaults: 'block',
                list: [
                    { value: 'block', name: 'Disable' },
                    { value: 'flex', name: 'Enable' }
                ],
            }, {
                name: 'Flex Parent',
                property: 'label-parent-flex',
                type: 'integer',
            }, {
                name: 'Direction',
                property: 'flex-direction',
                type: 'radio',
                defaults: 'row',
                list: [{
                    value: 'row',
                    name: 'Row',
                    className: 'icons-flex icon-dir-row',
                    title: 'Row',
                }, {
                    value: 'row-reverse',
                    name: 'Row reverse',
                    className: 'icons-flex icon-dir-row-rev',
                    title: 'Row reverse',
                }, {
                    value: 'column',
                    name: 'Column',
                    title: 'Column',
                    className: 'icons-flex icon-dir-col',
                }, {
                    value: 'column-reverse',
                    name: 'Column reverse',
                    title: 'Column reverse',
                    className: 'icons-flex icon-dir-col-rev',
                }],
            }, {
                name: 'Justify',
                property: 'justify-content',
                type: 'radio',
                defaults: 'flex-start',
                list: [{
                    value: 'flex-start',
                    className: 'icons-flex icon-just-start',
                    title: 'Start',
                }, {
                    value: 'flex-end',
                    title: 'End',
                    className: 'icons-flex icon-just-end',
                }, {
                    value: 'space-between',
                    title: 'Space between',
                    className: 'icons-flex icon-just-sp-bet',
                }, {
                    value: 'space-around',
                    title: 'Space around',
                    className: 'icons-flex icon-just-sp-ar',
                }, {
                    value: 'center',
                    title: 'Center',
                    className: 'icons-flex icon-just-sp-cent',
                }],
            }, {
                name: 'Align',
                property: 'align-items',
                type: 'radio',
                defaults: 'center',
                list: [{
                    value: 'flex-start',
                    title: 'Start',
                    className: 'icons-flex icon-al-start',
                }, {
                    value: 'flex-end',
                    title: 'End',
                    className: 'icons-flex icon-al-end',
                }, {
                    value: 'stretch',
                    title: 'Stretch',
                    className: 'icons-flex icon-al-str',
                }, {
                    value: 'center',
                    title: 'Center',
                    className: 'icons-flex icon-al-center',
                }],
            }, {
                name: 'Flex Children',
                property: 'label-parent-flex',
                type: 'integer',
            }, {
                name: 'Order',
                property: 'order',
                type: 'integer',
                defaults: 0,
                min: 0
            }, {
                name: 'Flex',
                property: 'flex',
                type: 'composite',
                properties: [{
                    name: 'Grow',
                    property: 'flex-grow',
                    type: 'integer',
                    defaults: 0,
                    min: 0
                }, {
                    name: 'Shrink',
                    property: 'flex-shrink',
                    type: 'integer',
                    defaults: 0,
                    min: 0
                }, {
                    name: 'Basis',
                    property: 'flex-basis',
                    type: 'integer',
                    units: ['px', '%', ''],
                    unit: '',
                    defaults: 'auto',
                }],
            }, {
                name: 'Align',
                property: 'align-self',
                type: 'radio',
                defaults: 'auto',
                list: [{
                    value: 'auto',
                    name: 'Auto',
                }, {
                    value: 'flex-start',
                    title: 'Start',
                    className: 'icons-flex icon-al-start',
                }, {
                    value: 'flex-end',
                    title: 'End',
                    className: 'icons-flex icon-al-end',
                }, {
                    value: 'stretch',
                    title: 'Stretch',
                    className: 'icons-flex icon-al-str',
                }, {
                    value: 'center',
                    title: 'Center',
                    className: 'icons-flex icon-al-center',
                }],
            }]
        }
        ],
    },
    plugins: [
        'gjs-blocks-basic',
        'grapesjs-plugin-forms',
        'grapesjs-component-countdown',
        'grapesjs-plugin-export',
        'grapesjs-tabs',
        'grapesjs-custom-code',
        'grapesjs-touch',
        'grapesjs-parser-postcss',
        'grapesjs-tooltip',
        'grapesjs-tui-image-editor',
        'grapesjs-typed',
        'grapesjs-style-bg',
        'grapesjs-preset-webpage',
        cosmos_plugin_array
    ],
    pluginsOpts: {
        'gjs-blocks-basic': { flexGrid: true },
        'grapesjs-tui-image-editor': {
            script: [
                // 'https://cdnjs.cloudflare.com/ajax/libs/fabric.js/1.6.7/fabric.min.js',
                'https://uicdn.toast.com/tui.code-snippet/v1.5.2/tui-code-snippet.min.js',
                'https://uicdn.toast.com/tui-color-picker/v2.2.7/tui-color-picker.min.js',
                'https://uicdn.toast.com/tui-image-editor/v3.15.2/tui-image-editor.min.js'
            ],
            style: [
                'https://uicdn.toast.com/tui-color-picker/v2.2.7/tui-color-picker.min.css',
                'https://uicdn.toast.com/tui-image-editor/v3.15.2/tui-image-editor.min.css',
            ],
        },
        'grapesjs-tabs': {
            tabsBlock: { category: 'Extra' }
        },
        'grapesjs-typed': {
            block: {
                category: 'Extra',
                content: {
                    type: 'typed',
                    'type-speed': 40,
                    strings: [
                        'Text row one',
                        'Text row two',
                        'Text row three',
                    ],
                }
            }
        },
        'grapesjs-preset-webpage': {
            modalImportTitle: 'Import Template',
            modalImportLabel: '<div style="margin-bottom: 10px; font-size: 13px;">Paste here your HTML/CSS and click Import</div>',
            modalImportContent: function (editor) {
                return editor.getHtml() + '<style>' + editor.getCss() + '</style>'
            },
        },
    },
    storageManager: {
        type: 'cosmos', // Storage type. Available: local | remote
        stepsBeforeSave: 1,
    },

});

var pn = editor.Panels;
var modal = editor.Modal;
var cmdm = editor.Commands;

// Update canvas-clear command
cmdm.add('canvas-clear', function () {
    if (confirm('Are you sure to clean the canvas?')) {
        editor.runCommand('core:canvas-clear')
        setTimeout(function () { localStorage.clear() }, 0)
    }
});

// Add info command
var mdlClass = 'gjs-mdl-dialog-sm';
var infoContainer = document.getElementById('info-panel');

cmdm.add('open-info', function () {
    var mdlDialog = document.querySelector('.gjs-mdl-dialog');
    mdlDialog.className += ' ' + mdlClass;
    infoContainer.style.display = 'block';
    modal.setTitle('About this demo');
    modal.setContent(infoContainer);
    modal.open();
    modal.getModel().once('change:open', function () {
        mdlDialog.className = mdlDialog.className.replace(mdlClass, '');
    })
});

pn.addButton('options', {
    id: 'open-info',
    className: 'fa fa-question-circle',
    command: function () { editor.runCommand('open-info') },
    attributes: {
        'title': 'About',
        'data-tooltip-pos': 'bottom',
    },
});

let displayViews = true;

pn.addButton('views', {
    id: 'toggle-views',
    className: 'fa-solid fa-toggle-on',
    command: function () {
        const btn = pn.getButton('views', 'toggle-views');
        if (displayViews) {
            document.querySelector('.gjs-pn-views-container').style.display = 'none';
            displayViews = false;
            btn.attributes.className = 'fa-solid fa-toggle-off';
        } else {
            document.querySelector('.gjs-pn-views-container').style.display = 'block';
            editor.runCommand('open-blocks');
            displayViews = true;
            btn.attributes.className = 'fa-solid fa-toggle-on';
        }
    },
    attributes: {
        'title': 'Toggle design pannel display.',
        'data-tooltip-pos': 'bottom',
    },
});

// Simple warn notifier
var origWarn = console.warn;
toastr.options = {
    closeButton: true,
    preventDuplicates: true,
    showDuration: 250,
    hideDuration: 150
};
console.warn = function (msg) {
    if (msg.indexOf('[undefined]') == -1) {
        toastr.warning(msg);
    }
    origWarn(msg);
};

// Add and beautify tooltips
[['sw-visibility', 'Show Borders'], ['preview', 'Preview'], ['fullscreen', 'Fullscreen'],
['export-template', 'Export'], ['undo', 'Undo'], ['redo', 'Redo'],
['gjs-open-import-webpage', 'Import'], ['canvas-clear', 'Clear canvas']]
    .forEach(function (item) {
        pn.getButton('options', item[0]).set('attributes', { title: item[1], 'data-tooltip-pos': 'bottom' });
    });
[['open-sm', 'Style Manager'], ['open-layers', 'Layers'], ['open-blocks', 'Blocks']]
    .forEach(function (item) {
        pn.getButton('views', item[0]).set('attributes', { title: item[1], 'data-tooltip-pos': 'bottom' });
    });
var titles = document.querySelectorAll('*[title]');

for (var i = 0; i < titles.length; i++) {
    var el = titles[i];
    var title = el.getAttribute('title');
    title = title ? title.trim() : '';
    if (!title)
        break;
    el.setAttribute('data-tooltip', title);
    el.setAttribute('title', '');
}

// Store and load events
editor.on('storage:load', function (e) { checkDisplayLiveEditorButton('HtmlContent');  console.log('Loaded ', e) });
editor.on('storage:store', function (e) { console.log('Stored ', e) });

// Do stuff on load
editor.on('load', function () {
    var $ = grapesjs.$;

    // Show borders by default
    pn.getButton('options', 'sw-visibility').set({
        command: 'core:component-outline',
        'active': true,
    });

    // Show logo with the version
    var logoCont = document.querySelector('.gjs-logo-cont');
    document.querySelector('.gjs-logo-version').innerHTML = 'v' + grapesjs.version;
    var logoPanel = document.querySelector('.gjs-pn-commands');
    logoPanel.appendChild(logoCont);


    // Load and show settings and style manager
    var openTmBtn = pn.getButton('views', 'open-tm');
    openTmBtn && openTmBtn.set('active', 1);
    var openSm = pn.getButton('views', 'open-sm');
    openSm && openSm.set('active', 1);

    // Remove trait view
    pn.removeButton('views', 'open-tm');

    // Add Settings Sector
    var traitsSector = $('<div class="gjs-sm-sector no-select">' +
        '<div class="gjs-sm-sector-title"><span class="icon-settings fa fa-cog"></span> <span class="gjs-sm-sector-label">Settings</span></div>' +
        '<div class="gjs-sm-properties" style="display: none;"></div></div>');
    var traitsProps = traitsSector.find('.gjs-sm-properties');
    traitsProps.append($('.gjs-traits-cs'));
    $('.gjs-sm-sectors').before(traitsSector);
    traitsSector.find('.gjs-sm-sector-title').on('click', function () {
        var traitStyle = traitsProps.get(0).style;
        var hidden = traitStyle.display == 'none';
        if (hidden) {
            traitStyle.display = 'block';
        } else {
            traitStyle.display = 'none';
        }
    });

    // Open block manager
    var openBlocksBtn = editor.Panels.getButton('views', 'open-blocks');
    openBlocksBtn && openBlocksBtn.set('active', 1);

});

editor.on('storage:start:load', () => {
    $("#spinLoading").show();
});

editor.on('storage:load', (data, res) => {
    $("#spinLoading").hide();
});

// Add the CKEditor editing block
// Unmovable container
editor.DomComponents.addType('ccms-ckeditor-block', {
    model: {
        defaults: {
            attributes: {
                // Default attributes
                type: 'text',
                name: 'data-ccms-ceid',
                placeholder: ccms___generateGUID(),
            },
        }
    }
});
//data-ccms-ceid: ccms___generateGUID(), class: 'ck-content' 